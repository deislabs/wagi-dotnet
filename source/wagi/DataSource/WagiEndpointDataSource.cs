#pragma warning disable CA1001
#pragma warning disable CA1812
#pragma warning disable CA2008
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Deislabs.Wagi.Configuration;
using Deislabs.Wagi.Extensions;
using Deislabs.Wagi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Wasi.Experimental.Http;
using Wasmtime;

namespace Deislabs.Wagi.DataSource
{
    internal class WagiEndpointDataSource : EndpointDataSource, IEndpointConventionBuilder
    {
        const string RoutesEntryPoint = "_routes";
        private CancellationTokenSource cancellationTokenSource;
        private IChangeToken changeToken;
        private List<Endpoint> endpoints;
        private readonly List<Action<EndpointBuilder>> conventions = new();
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<WagiEndpointDataSource> logger;
        private readonly ILoggerFactory loggerFactory;
        private readonly IOptionsMonitor<WagiModules> optionsManager;
        private readonly int defaultHttpRequestLimit;
        private static Lazy<ModuleResolver> moduleResolver;
        private const int QueueCapacity = 128;

        public override IReadOnlyList<Endpoint> Endpoints
        {
            get
            {
                return endpoints;
            }
        }

        private readonly Channel<WagiModules> queue;

        public WagiEndpointDataSource(ILogger<WagiEndpointDataSource> logger, IHttpClientFactory httpClientFactory, IOptionsMonitor<WagiModules> optionsManager, ILoggerFactory loggerFactory)
        {
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
            this.loggerFactory = loggerFactory;
            this.optionsManager = optionsManager;
            var options = new BoundedChannelOptions(QueueCapacity) { FullMode = BoundedChannelFullMode.Wait };
            this.queue = Channel.CreateBounded<WagiModules>(options);
            this.optionsManager.OnChange<WagiModules>(async modules =>
            {
                logger.TraceMessage("Configuration OnChange called.");
                await queue.Writer.WriteAsync(modules);

            });

            var wagiConfig = optionsManager.CurrentValue;
            string cacheConfig = null;
            if (!string.IsNullOrEmpty(wagiConfig.CacheConfigPath))
            {
                var message = $"Using {wagiConfig.CacheConfigPath} as cache configuration";
                logger.TraceMessage(message);
                cacheConfig = wagiConfig.CacheConfigPath;
            }

            moduleResolver = new Lazy<ModuleResolver>(() =>
                {
                    var config = new Config();
                    return new ModuleResolver(config.WithCacheConfig(cacheConfig));
                },
                LazyThreadSafetyMode.ExecutionAndPublication
            );

            this.defaultHttpRequestLimit = wagiConfig.MaxHttpRequests > 0 ? wagiConfig.MaxHttpRequests : HttpRequestHandler.DefaultHttpRequestLimit;

            // It is valid to have no modules defined when being used in Hippo.
            var hasModuleDefinitions = wagiConfig.Modules?.Any() ?? default;
            var hasBindleDefinitions = wagiConfig.Bindles?.Any() ?? default;
            if (!hasModuleDefinitions && !hasBindleDefinitions)
            {
                logger.TraceWarning("No modules found in configuration.");
                this.endpoints = new();
            }
            else
            {
                this.endpoints = BuildEndpoints(wagiConfig).Result;
            }

            this.cancellationTokenSource = new CancellationTokenSource();
            this.changeToken = new CancellationChangeToken(this.cancellationTokenSource.Token);
            var taskScheduler = TaskScheduler.Default;
            var taskFactory = new TaskFactory(taskScheduler);
            taskFactory.StartNew(async () =>
            {
                var updateCount = 0;
                while (true)
                {
                    var moreUpdates = false;
                    var modules = await queue.Reader.ReadAsync();
                    do
                    {
                        updateCount++;
                        var message = $"Processing Config Change Request: {updateCount}";
                        logger.TraceMessage(message);

                        // This is to deal with the fact that the change event fires multiple times for one change. We only care about one (the last one).
                        Thread.Sleep(500);
                        moreUpdates = queue.Reader.TryRead(out var updateModules);
                        if (moreUpdates)
                        {
                            logger.TraceMessage("Found another update, will wait for 500 milliseconds to see if it is the last one.");
                            modules = updateModules;
                        }

                    } while (moreUpdates);
                    var endpoints = await BuildEndpoints(modules);
                    UpdateDataAndSignalChange(endpoints);
                }
            });
        }

        private void UpdateDataAndSignalChange(List<Endpoint> endpoints)
        {
            this.endpoints = endpoints;
            var tokenSource = this.cancellationTokenSource;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.changeToken = new CancellationChangeToken(this.cancellationTokenSource.Token);
            tokenSource.Cancel();
        }

        private async Task<List<Endpoint>> BuildEndpoints(WagiModules modules)
        {
            var endpoints = new List<Endpoint>();

            if (modules.Bindles?.Any() ?? default)
            {
                await LoadBindles(modules, this.loggerFactory);
            }

            foreach (var module in modules.Modules)
            {
                var name = module.Key;
                var route = module.Value.Route;
                var originalRoute = module.Value.Route;
                route = CheckForWildcardRoute(route);
                var wagiModuleInfo = module.Value;
                var fileName = wagiModuleInfo.FileName;
                var moduleFileAndPath = Path.Join(modules.ModulePath, fileName);
                var moduleType = fileName.Split('.')[1].ToUpperInvariant();
                var httpMethod = GetHTTPMethod(wagiModuleInfo.HttpMethod);
                var allowedHosts = new List<Uri>();
                if (wagiModuleInfo.AllowedHosts?.Count > 0)
                {
                    foreach (var allowedHost in wagiModuleInfo.AllowedHosts)
                    {
                        allowedHosts.Add(new Uri(allowedHost));
                    }
                }

                var maxHttpRequests = (wagiModuleInfo.MaxHttpRequests > 0 && wagiModuleInfo.MaxHttpRequests < HttpRequestHandler.MaxHttpRequestLimit) ? wagiModuleInfo.MaxHttpRequests : defaultHttpRequestLimit;
                var hostnames = wagiModuleInfo?.Hostnames is null ? string.Empty : string.Join(",", wagiModuleInfo.Hostnames);

                var endPointBuilder = GetEndpointBuilder(name, route, moduleFileAndPath, wagiModuleInfo, wagiModuleInfo.Entrypoint, hostnames, originalRoute, async context =>
                {
                    await context.RunWAGIRequest(moduleFileAndPath, this.httpClientFactory, wagiModuleInfo.Entrypoint, moduleResolver.Value, wagiModuleInfo.Volumes, wagiModuleInfo.Environment, allowedHosts, maxHttpRequests);
                });
                var endPoint = endPointBuilder.Build();
                endpoints.Add(endPoint);

                // Check for _routes entrypoint

                try
                {
                    var wasmModule = moduleResolver.Value.GetWasmModule(moduleFileAndPath);
                    var moduleExposesRoutes = wasmModule.Exports.ToList<Export>().Exists(f => (f.Name == RoutesEntryPoint && f is FunctionExport));
                    if (moduleExposesRoutes)
                    {

                        var routes = await GetRouteDetails(wasmModule);
                        foreach (var moduleRoutes in routes)
                        {
                            logger.AddingModuleDefinedRoute(moduleRoutes.route, moduleRoutes.entryPoint);
                            var resultantName = $"{name}/{moduleRoutes.route}".Replace("//", "/", StringComparison.InvariantCulture);
                            var resultantOriginalRoute = $"{originalRoute.Trim('.')}/{moduleRoutes.route.TrimStart('/')}".Replace("//", "/", StringComparison.InvariantCulture);
                            var resultantRoute = CheckForWildcardRoute(resultantOriginalRoute);
                            endPointBuilder = GetEndpointBuilder(resultantName, resultantRoute, moduleFileAndPath, wagiModuleInfo, moduleRoutes.entryPoint, hostnames, resultantOriginalRoute, async context =>
                            {
                                await context.RunWAGIRequest(moduleFileAndPath, this.httpClientFactory, moduleRoutes.entryPoint, moduleResolver.Value, wagiModuleInfo.Volumes, wagiModuleInfo.Environment, allowedHosts, maxHttpRequests);
                            });
                            endPoint = endPointBuilder.Build();
                            endpoints.Add(endPoint);
                            logger.AddedRoute(moduleRoutes.route, moduleRoutes.entryPoint);
                        }
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    logger.FailedToAddModuleDefinedRoute(name, ex);
                }
            }

            return endpoints;
        }

        private static async Task<List<(string route, string entryPoint)>> GetRouteDetails(Module wasmModule)
        {
            var routes = new List<(string route, string entryPoint)>();
            var engine = moduleResolver.Value.Engine;
            using var linker = new Linker(engine);
            using var store = new Store(engine);
            using var stdin = new TempFile();
            using var stdout = new TempFile();
            using var stderr = new TempFile();
            var config = new WasiConfiguration()
              .WithStandardOutput(stdout.Path)
              .WithStandardInput(stdin.Path)
              .WithStandardError(stderr.Path);
            store.SetWasiConfiguration(config);
            linker.DefineWasi();
            var instance = linker.Instantiate(store, wasmModule);
            var entrypoint = instance.GetFunction(store, RoutesEntryPoint);
            entrypoint.Invoke(store);
            using var stdoutStream = new FileStream(stdout.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stdoutStream);
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var routeDetails = line.Split(" ");
                routes.Add((routeDetails[0], routeDetails[1]));
            }
            return routes;
        }

        private string CheckForWildcardRoute(string originalRoute)
        {
            var route = originalRoute;
            if (originalRoute.EndsWith("/...", StringComparison.InvariantCulture))
            {
                route = $"{originalRoute.TrimEnd('.')}{{**path}}";
                logger.MappedWildcard(originalRoute, route);
            }
            return route;
        }

        private EndpointBuilder GetEndpointBuilder(string name, string route, string moduleFileAndPath, WagiModuleInfo wagiModuleInfo, string entryPoint, string hostnames, string originalRoute, RequestDelegate requestDelegate)
        {
            logger.TraceMessage($"Adding Route Endpoint for Module: {name} File: {moduleFileAndPath} Entrypoint: {entryPoint ?? "Default"} Route:{route} Hostnames: {hostnames}");
            var pattern = RoutePatternFactory.Parse(route);
            var endPointBuilder = new RouteEndpointBuilder(requestDelegate, pattern, 1);
            foreach (var convention in this.conventions)
            {
                convention(endPointBuilder);
            }

            endPointBuilder.DisplayName = name;
            endPointBuilder.Metadata.Add(new WagiRouteAttribute(originalRoute));

            if (wagiModuleInfo.Hostnames != null && wagiModuleInfo.Hostnames.Any())
            {
                endPointBuilder.Metadata.Add(new HostAttribute(wagiModuleInfo.Hostnames.ToArray()));
            }

            if (wagiModuleInfo.Policies?.Count > 0 || wagiModuleInfo.Roles?.Count > 0)
            {
                if (wagiModuleInfo.Policies?.Count > 0)
                {
                    foreach (var policy in wagiModuleInfo.Policies)
                    {
                        endPointBuilder.Metadata.Add(new AuthorizeAttribute(policy));
                    }
                }

                if (wagiModuleInfo.Roles?.Count > 0)
                {
                    var authData = new AuthorizeAttribute
                    {
                        Roles = string.Join(',', wagiModuleInfo.Roles.ToArray<string>()),
                    };
                    endPointBuilder.Metadata.Add(authData);
                }
            }
            else if (wagiModuleInfo.Authorize)
            {
                endPointBuilder.Metadata.Add(new AuthorizeAttribute());
            }

            return endPointBuilder;
        }

        public override IChangeToken GetChangeToken()
        {
            return this.changeToken;
        }
        public void Add(Action<EndpointBuilder> convention)
        {
            this.conventions.Add(convention);
        }

        private static async Task LoadBindles(WagiModules modules, ILoggerFactory loggerFactory)
        {
            var bindleResolver = new BindleResolver(modules, loggerFactory);
            await bindleResolver.LoadInvoice();
        }
        private static string GetHTTPMethod(string httpMethod) => string.IsNullOrEmpty(httpMethod) ? "GET" : httpMethod;

    }
    /// <summary>
    /// WagiRouteAttribute enables route metadata to include the original route specified in the configuration
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1018:Mark attributes with AttributeUsageAttribute", Justification = "Not needed")]
    sealed class WagiRouteAttribute : Attribute
    {
        public string Route { get; private set; }
        public WagiRouteAttribute(string route)
        {
            Route = route;
        }
    }
}
#pragma warning restore CA1001
#pragma warning restore CA1812
#pragma warning restore CA2008
