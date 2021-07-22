namespace Deislabs.WAGI.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using Deislabs.WAGI.Configuration;
    using Deislabs.WAGI.DataSource;
    using Deislabs.WAGI.Helpers;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Wasi.Experimental.Http;
    using Wasmtime;

    /// <summary>
    /// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to add routes.
    /// </summary>
    public static class EndpointRouteBuilderExtensions
    {

        /// <summary>
        /// Adds a route endpoint to the <see cref="IEndpointRouteBuilder"/> for each WASM Function defined in configuration.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> instance being extended. </param>
        /// <returns>IEndpointConventionBuilder to configure endpoints.</returns>
        public static IEndpointConventionBuilder MapWagiModules(this IEndpointRouteBuilder endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            var dataSource = endpoints.ServiceProvider.GetRequiredService<WagiEndpointDataSource>();
            if (dataSource == null)
            {
                throw new InvalidOperationException("Unable to find the required services. Please add all the required services by calling 'IServiceCollection.AddWASM(...)' inside the call to 'ConfigureServices(...)' in the application startup code.");
            }

            endpoints.DataSources.Add(dataSource);
            return dataSource;
        }


        private static Lazy<ModuleResolver> moduleResolver;
        /// <summary>
        /// Adds a route endpoint to the <see cref="IEndpointRouteBuilder"/> for each WASM Function defined in configuration.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> instance being extended. </param>
        /// <returns>IEndpointConventionBuilder to configure endpoints.</returns>
        public static IEndpointConventionBuilder MapWASMModules(
          this IEndpointRouteBuilder endpoints)
        {
            var loggerFactory = endpoints?.ServiceProvider.GetService<ILoggerFactory>();
            var httpClientFactory = endpoints.ServiceProvider.GetService<IHttpClientFactory>();
            var logger = loggerFactory.CreateLogger(typeof(EndpointRouteBuilderExtensions).FullName);
            var endpointConventionBuilders = new List<IEndpointConventionBuilder>();
            var configuration = endpoints.ServiceProvider.GetService<IConfiguration>();
            var modules = endpoints.ServiceProvider.GetService<WASMModules>();
            if (modules == null)
            {
                throw new InvalidOperationException("Unable to find the required services. Please add all the required services by calling 'IServiceCollection.AddWASM(...)' inside the call to 'ConfigureServices(...)' in the application startup code.");
            }
            else
            {
                var optionsManager = endpoints.ServiceProvider.GetService<IOptionsMonitor<WASMModules>>();
                optionsManager.OnChange<WASMModules>((modules) =>
                {
                    logger.LogTrace($"Configuration has changed");
                });

                if (modules.Bindles?.Any() ?? default)
                {
                    LoadBindles(modules, loggerFactory);
                }

                string cacheConfig = null;
                if (!string.IsNullOrEmpty(modules.CacheConfigPath))
                {
                    logger.LogTrace($"Using {modules.CacheConfigPath} as cache configuration");
                    cacheConfig = modules.CacheConfigPath;
                }

                moduleResolver = new Lazy<ModuleResolver>(() =>
                    {
                        var config = new Config();
                        return new ModuleResolver(config.WithCacheConfig(cacheConfig));
                    },
                    LazyThreadSafetyMode.ExecutionAndPublication
                );

                var defaultHttpRequestLimit = modules.MaxHttpRequests > 0 ? modules.MaxHttpRequests : HttpRequestHandler.DefaultHttpRequestLimit;
                var hasModuleDefinitions = modules.Modules?.Any() ?? default;
                if (!hasModuleDefinitions)
                {
                    logger.LogWarning("No module definitions found in configuration. No endpoints will be added.");
                }

                foreach (var module in modules.Modules)
                {
                    var route = module.Key;
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

                    var maxHttpRequests = defaultHttpRequestLimit;
                    if (moduleDetails.MaxHttpRequests > 0 && moduleDetails.MaxHttpRequests < HttpRequestHandler.MaxHttpRequestLimit)
                    {
                        maxHttpRequests = moduleDetails.MaxHttpRequests;
                    }

                    logger.LogTrace($"Added Route Endpoint for Route: {route} File: {moduleFileAndPath} Entrypoint: {moduleDetails.Entrypoint ?? "Default"}");
                    var endpointConventionBuilder = endpoints.MapMethods(route, new string[] { httpMethod }, async context =>
                    {
                        await context.RunWAGIRequest(moduleFileAndPath, httpClientFactory, moduleDetails.Entrypoint, moduleResolver.Value, moduleDetails.Volumes, moduleDetails.Environment, allowedHosts, maxHttpRequests);
                    });

                    if (moduleDetails.Policies?.Count > 0 || moduleDetails.Roles?.Count > 0)
                    {
                        if (moduleDetails.Policies?.Count > 0)
                        {
                            endpointConventionBuilder.RequireAuthorization(moduleDetails.Policies.ToArray<string>());
                        }

                        if (moduleDetails.Roles?.Count > 0)
                        {
                            var authData = new AuthorizeAttribute
                            {
                                Roles = string.Join(',', moduleDetails.Roles.ToArray<string>()),
                            };
                            endpointConventionBuilder.RequireAuthorization(authData);
                        }
                    }
                    else if (moduleDetails.Authorize)
                    {
                        endpointConventionBuilder.RequireAuthorization();
                    }

                    endpointConventionBuilders.Add(endpointConventionBuilder);
                }

            }

            return new WagiEndPointConventionBuilder(endpointConventionBuilders);
        }

        private static void LoadBindles(WASMModules modules, ILoggerFactory loggerFactory)
        {
            var bindleResolver = new BindleResolver(modules, loggerFactory);
            bindleResolver.LoadInvoice().Wait();
        }

        private static string GetHTTPMethod(string httpMethod) => string.IsNullOrEmpty(httpMethod) ? "GET" : httpMethod;
    }
}
