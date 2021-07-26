namespace Deislabs.WAGI.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using Deislabs.WAGI.Configuration;
    using Deislabs.WAGI.Helpers;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Wasi.Experimental.Http;
    using Wasmtime;

    /// <summary>
    /// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to add routes.
    /// </summary>
    public static class EndpointRouteBuilderExtensions
    {
        private static Lazy<ModuleResolver> moduleResolver;
        /// <summary>
        /// Adds a route endpoint to the <see cref="IEndpointRouteBuilder"/> for each WASM Function defined in configuration.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> instance being extended. </param>
        /// <param name="section">The configuration section containing the modules to be processed. </param>
        /// <returns>IEndpointConventionBuilder to configure endpoints.</returns>
        public static IEndpointConventionBuilder MapWASMModules(
          this IEndpointRouteBuilder endpoints,
          string section = "WASM")
        {
            var loggerFactory = endpoints?.ServiceProvider.GetService<ILoggerFactory>();
            var httpClientFactory = endpoints?.ServiceProvider.GetService<IHttpClientFactory>();
            var logger = loggerFactory.CreateLogger(typeof(EndpointRouteBuilderExtensions).FullName);
            var endpointConventionBuilders = new List<IEndpointConventionBuilder>();
            var configuration = endpoints.ServiceProvider.GetService<IConfiguration>();
            var modules = new WASMModules();
            var moduleConfig = configuration.GetSection(section);
            if (!moduleConfig.Exists())
            {
                logger.LogError($"No configuration found in section {section}");
            }
            else
            {
                moduleConfig.Bind(modules);
                if (modules.Bindles?.Any() ?? default)
                {
                    LoadBindles(modules, loggerFactory);
                }

                if (!Directory.Exists(modules.ModulePath))
                {
                    throw new ApplicationException($"Module Path not found {modules.ModulePath}");
                }

                string cacheConfig = null;
                if (!string.IsNullOrEmpty(modules.CacheConfigPath))
                {
                    if (File.Exists(modules.CacheConfigPath))
                    {
                        logger.LogTrace($"Using {modules.CacheConfigPath} as cache configuration");
                        cacheConfig = modules.CacheConfigPath;
                    }
                    else
                    {
                        logger.LogError($"Wasmtime cache config file {modules.CacheConfigPath} does not exist");
                    }
                }

                moduleResolver = new Lazy<ModuleResolver>(() =>
                    {
                        var config = new Config();
                        return new ModuleResolver(config.WithCacheConfig(cacheConfig));
                    },
                    LazyThreadSafetyMode.ExecutionAndPublication
                );

                var defaultHttpRequestLimit = HttpRequestHandler.DefaultHttpRequestLimit;
                if (modules.MaxHttpRequests > 0 && modules.MaxHttpRequests < HttpRequestHandler.MaxHttpRequestLimit)
                {
                    defaultHttpRequestLimit = modules.MaxHttpRequests;
                }

                if (modules.Modules == null || modules.Modules.Count == 0)
                {
                    logger.LogError($"No Module configuration found in section {section}");
                }
                else
                {
                    foreach (var module in modules.Modules)
                    {
                        var name = module.Key;
                        var route = module.Value.Route;
                        if (string.IsNullOrEmpty(route))
                        {
                            logger.LogError($"Route should not be null or empty Module details {name}");
                            continue;
                        }

                        if (route.Contains("{", StringComparison.InvariantCulture) && route.Contains("}", StringComparison.InvariantCulture))
                        {
                            logger.LogError($"Route cannot contain either {{ or }} {route}- skipping");
                            continue;
                        }

                        var moduleDetails = module.Value ?? throw new ApplicationException($"Missing module details for module details {name}");
                        var fileName = moduleDetails.FileName ?? throw new ApplicationException($"Missing module file name for module details {name}");
                        var moduleFileAndPath = Path.Join(modules.ModulePath, fileName);
                        if (!File.Exists(moduleFileAndPath))
                        {
                            logger.LogError($"Module file {moduleFileAndPath} not found for module details {name} - skipping");
                            continue;
                        }

                        var moduleType = fileName.Split('.')[1].ToUpperInvariant();
                        if (moduleType != "WAT" && moduleType != "WASM")
                        {
                            throw new ApplicationException($"Module Filename extension should be either .wat or .wasm Filename: {fileName} Module details {name}");
                        }

                        if (!File.Exists(moduleFileAndPath))
                        {
                            throw new ApplicationException($"File {moduleFileAndPath} not found for Module details {name}");
                        }

                        var httpMethod = GetHTTPMethod(moduleDetails.HttpMethod, name);
                        var allowedHosts = new List<Uri>();
                        if (moduleDetails.AllowedHosts?.Count > 0)
                        {
                            foreach (var allowedHost in moduleDetails.AllowedHosts)
                            {
                                if (Uri.TryCreate(allowedHost, UriKind.Absolute, out var uri))
                                {
                                    allowedHosts.Add(uri);
                                }
                                else
                                {
                                    logger.LogError($"failed to create Uri for allowed host {allowedHost}  for module details {name}-skipping");
                                }
                            }
                        }

                        var maxHttpRequests = defaultHttpRequestLimit;
                        if (moduleDetails.MaxHttpRequests > 0 && moduleDetails.MaxHttpRequests < HttpRequestHandler.MaxHttpRequestLimit)
                        {
                            maxHttpRequests = moduleDetails.MaxHttpRequests;
                        }

                        logger.LogTrace($"Adding Route Endpoint for Module: {name} File: {moduleFileAndPath} Entrypoint: {moduleDetails.Entrypoint ?? "Default"} Route:{route} Hostnames: {moduleDetails.Hostnames}");
                        var endpointConventionBuilder = endpoints.MapMethods(route, new string[] { httpMethod }, async context =>
                        {
                            await context.RunWAGIRequest(moduleFileAndPath, httpClientFactory, moduleDetails.Entrypoint, moduleResolver.Value, moduleDetails.Volumes, moduleDetails.Environment, allowedHosts, maxHttpRequests);
                        });

                        if (moduleDetails.Hostnames != null && moduleDetails.Hostnames.Any())
                        {
                            endpointConventionBuilder.RequireHost(moduleDetails.Hostnames.ToArray());
                        }

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
            }

            return new WagiEndPointConventionBuilder(endpointConventionBuilders);
        }

        private static void LoadBindles(WASMModules modules, ILoggerFactory loggerFactory)
        {
            var bindleResolver = new BindleResolver(modules, loggerFactory);
            bindleResolver.LoadInvoice().Wait();
        }

        private static string GetHTTPMethod(string httpMethod, string name)
        {
            if (!string.IsNullOrEmpty(httpMethod))
            {
                if (httpMethod.ToUpperInvariant() != "GET" && httpMethod.ToUpperInvariant() != "POST")
                {
                    throw new ApplicationException($"Module HttpMethod should be either GET or POST for Module details {name}");
                }

                return httpMethod;
            }

            return "GET";
        }
    }
}
