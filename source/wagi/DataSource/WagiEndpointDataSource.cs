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
using Deislabs.WAGI.Configuration;
using Deislabs.WAGI.Extensions;
using Deislabs.WAGI.Helpers;
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

namespace Deislabs.WAGI.DataSource
{
    internal class WagiEndpointDataSource : EndpointDataSource, IEndpointConventionBuilder
    {
        private CancellationTokenSource cancellationTokenSource;
        private IChangeToken changeToken;
        private List<Endpoint> endpoints;
        private readonly List<Action<EndpointBuilder>> conventions = new();
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<WagiEndpointDataSource> logger;
        private readonly ILoggerFactory loggerFactory;
        private readonly IOptionsMonitor<WASMModules> optionsManager;
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

        private readonly Channel<WASMModules> queue;

        public WagiEndpointDataSource(ILogger<WagiEndpointDataSource> logger, IHttpClientFactory httpClientFactory, IOptionsMonitor<WASMModules> optionsManager, ILoggerFactory loggerFactory)
        {
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
            this.loggerFactory = loggerFactory;
            this.optionsManager = optionsManager;
            var options = new BoundedChannelOptions(QueueCapacity) { FullMode = BoundedChannelFullMode.Wait };
            this.queue = Channel.CreateBounded<WASMModules>(options);
            this.optionsManager.OnChange<WASMModules>(async modules =>
            {
                logger.LogTrace("Configuration OnChange called.");
                await queue.Writer.WriteAsync(modules);

            });

            var wasmConfig = optionsManager.CurrentValue;
            string cacheConfig = null;
            if (!string.IsNullOrEmpty(wasmConfig.CacheConfigPath))
            {
                logger.LogTrace($"Using {wasmConfig.CacheConfigPath} as cache configuration");
                cacheConfig = wasmConfig.CacheConfigPath;
            }

            moduleResolver = new Lazy<ModuleResolver>(() =>
                {
                    var config = new Config();
                    return new ModuleResolver(config.WithCacheConfig(cacheConfig));
                },
                LazyThreadSafetyMode.ExecutionAndPublication
            );

            this.defaultHttpRequestLimit = wasmConfig.MaxHttpRequests > 0 ? wasmConfig.MaxHttpRequests : HttpRequestHandler.DefaultHttpRequestLimit;

            // It is valid to have no modules defined when being used in Hippo.
            var hasModuleDefinitions = wasmConfig.Modules?.Any() ?? default;
            var hasBindleDefinitions = wasmConfig.Bindles?.Any() ?? default;
            if (!hasModuleDefinitions && !hasBindleDefinitions)
            {
                logger.LogWarning("No modules found in configuration.");
                this.endpoints = new();
            }
            else
            {
                this.endpoints = BuildEndpoints(wasmConfig);
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
                        logger.LogTrace($"Processing Config Change Request: {updateCount}");

                        // This is to deal with the fact that the change event fires multiple times for one change. We only care about one (the last one).
                        Thread.Sleep(500);
                        moreUpdates = queue.Reader.TryRead(out var updateModules);
                        if (moreUpdates)
                        {
                            logger.LogTrace("Found another update, will wait for 500 milliseconds to see if it is the last one.");
                            modules = updateModules;
                        }

                    } while (moreUpdates);
                    var endpoints = BuildEndpoints(modules);
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

        private List<Endpoint> BuildEndpoints(WASMModules modules)
        {
            var endpoints = new List<Endpoint>();

            if (modules.Bindles?.Any() ?? default)
            {
                LoadBindles(modules, this.loggerFactory);
            }

            // order of the items could be important.

            var order = 1;
            foreach (var module in modules.Modules)
            {
                var name = module.Key;
                var route = module.Value.Route;
                if (route.EndsWith("/...", StringComparison.InvariantCulture))
                {
                    route = $"{route.TrimEnd('.')}{{**path}}";
                }

                var moduleDetails = module.Value;
                var fileName = moduleDetails.FileName;
                var moduleFileAndPath = Path.Join(modules.ModulePath, fileName);
                var moduleType = fileName.Split('.')[1].ToUpperInvariant();
                var httpMethod = GetHTTPMethod(moduleDetails.HttpMethod);
                var allowedHosts = new List<Uri>();
                if (moduleDetails.AllowedHosts?.Count > 0)
                {
                    foreach (var allowedHost in moduleDetails.AllowedHosts)
                    {
                        allowedHosts.Add(new Uri(allowedHost));
                    }
                }

                var maxHttpRequests = (moduleDetails.MaxHttpRequests > 0 && moduleDetails.MaxHttpRequests < HttpRequestHandler.MaxHttpRequestLimit) ? moduleDetails.MaxHttpRequests : defaultHttpRequestLimit;
                var hostnames = moduleDetails?.Hostnames is null ? string.Empty : string.Join(",", moduleDetails.Hostnames);
                logger.LogTrace($"Adding Route Endpoint for Module: {name} File: {moduleFileAndPath} Entrypoint: {moduleDetails.Entrypoint ?? "Default"} Route:{route} Hostnames: {hostnames}");
                var pattern = RoutePatternFactory.Parse(route);
                var endPointBuilder = new RouteEndpointBuilder(
                    async context =>
                    {
                        await context.RunWAGIRequest(moduleFileAndPath, this.httpClientFactory, moduleDetails.Entrypoint, moduleResolver.Value, moduleDetails.Volumes, moduleDetails.Environment, allowedHosts, maxHttpRequests);
                    },
                    pattern,
                    order++);

                foreach (var convention in this.conventions)
                {
                    convention(endPointBuilder);
                }

                if (moduleDetails.Hostnames != null && moduleDetails.Hostnames.Any())
                {
                    endPointBuilder.Metadata.Add(new HostAttribute(moduleDetails.Hostnames.ToArray()));
                }

                if (moduleDetails.Policies?.Count > 0 || moduleDetails.Roles?.Count > 0)
                {
                    if (moduleDetails.Policies?.Count > 0)
                    {
                        foreach (var policy in moduleDetails.Policies)
                        {
                            endPointBuilder.Metadata.Add(new AuthorizeAttribute(policy));
                        }
                    }

                    if (moduleDetails.Roles?.Count > 0)
                    {
                        var authData = new AuthorizeAttribute
                        {
                            Roles = string.Join(',', moduleDetails.Roles.ToArray<string>()),
                        };
                        endPointBuilder.Metadata.Add(authData);
                    }
                }
                else if (moduleDetails.Authorize)
                {
                    endPointBuilder.Metadata.Add(new AuthorizeAttribute());
                }

                var endPoint = endPointBuilder.Build();
                endpoints.Add(endPoint);
            }

            return endpoints;
        }

        public override IChangeToken GetChangeToken()
        {
            return this.changeToken;
        }
        public void Add(Action<EndpointBuilder> convention)
        {
            this.conventions.Add(convention);
        }

        private static void LoadBindles(WASMModules modules, ILoggerFactory loggerFactory)
        {
            var bindleResolver = new BindleResolver(modules, loggerFactory);
            bindleResolver.LoadInvoice().Wait();
        }
        private static string GetHTTPMethod(string httpMethod) => string.IsNullOrEmpty(httpMethod) ? "GET" : httpMethod;

    }
}
#pragma warning restore CA1001
#pragma warning restore CA1812
#pragma warning restore CA2008
