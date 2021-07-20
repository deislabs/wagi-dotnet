using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using Microsoft.Extensions.Options;
using Wasi.Experimental.Http;

namespace Deislabs.WAGI.Configuration
{
    /// <summary>
    /// Validates configuration for WAGI dotnet
    /// </summary>
    public class ValidateConfiguration : IValidateOptions<WASMModules>
    {
        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string name, WASMModules options)
        {
            _ = options ?? throw new ArgumentException("Options should not be Null");
            StringBuilder result = new();
            var hasModuleDefinitions = options.Modules?.Any() ?? default;
            var hasBindleDefinitions = options.Bindles?.Any() ?? default;
            if (string.IsNullOrEmpty(options.ModulePath))
            {
                options.ModulePath = "modules";
            }

            if (hasModuleDefinitions && !Directory.Exists(options.ModulePath))
            {
                result.AppendLine("Module Path not found {options.ModulePath}");
            }

            if (!string.IsNullOrEmpty(options.CacheConfigPath) && !File.Exists(options.CacheConfigPath))
            {
                result.AppendLine($"Wasmtime cache config file {options.CacheConfigPath} does not exist");
            }

            if (!string.IsNullOrEmpty(options.BindleServer) && !Uri.TryCreate(options.BindleServer, UriKind.Absolute, out _))
            {
                result.AppendLine($"Bindle Server is Invalid");
            }

            // TODO: might need to allow this for Hippo with no defined channels  
            if (!hasBindleDefinitions && !hasModuleDefinitions)
            {
                result.AppendLine($"Configuration contains no module or bindle configs");
            }

            if (string.IsNullOrEmpty(options.BindleServer) && hasBindleDefinitions)
            {
                result.AppendLine($"Bindle Server is not configured but there are bindle definitions");
            }

            if (options.MaxHttpRequests > HttpRequestHandler.MaxHttpRequestLimit)
            {
                result.AppendLine($"MaxHttpRequests of {options.MaxHttpRequests} not allowed - maximum is {HttpRequestHandler.MaxHttpRequestLimit}");
            }

            if (hasModuleDefinitions)
            {
                foreach (var module in options.Modules)
                {
                    var route = module.Key;
                    if (route.Contains("{", StringComparison.InvariantCulture) && route.Contains("}", StringComparison.InvariantCulture))
                    {
                        result.AppendLine($"Module route '{route}' cannot contain either {{ or }} {route}");
                    }

                    if (module.Value is null)
                    {
                        result.AppendLine($"Missing module details for route {route}");
                    }

                    if (string.IsNullOrEmpty(module.Value.FileName))
                    {
                        result.AppendLine($"Missing module file name for route {route}");
                    }

                    var moduleType = module.Value.FileName.Split('.')[1].ToUpperInvariant();
                    if (moduleType != "WAT" && moduleType != "WASM")
                    {
                        result.AppendLine($"Module Filename extension should be either .wat or .wasm Filename: {module.Value.FileName} Route:{route}");
                    }

                    var moduleFileAndPath = Path.Join(options.ModulePath, module.Value.FileName);
                    if (!File.Exists(moduleFileAndPath))
                    {
                        result.AppendLine($"Module file {moduleFileAndPath} not found for route {route}");
                    }

                    if (!string.IsNullOrEmpty(module.Value.HttpMethod) && module.Value.HttpMethod.ToUpperInvariant() != "GET" && module.Value.HttpMethod.ToUpperInvariant() != "POST")
                    {
                        result.AppendLine($"Module HttpMethod should be either GET or POST for Route:{route}");
                    }

                    if (module.Value.AllowedHosts?.Count > 0)
                    {
                        foreach (var allowedHost in module.Value.AllowedHosts)
                        {
                            if (!Uri.TryCreate(allowedHost, UriKind.Absolute, out var _))
                            {
                                result.AppendLine($"Invalid Uri for allowed host {allowedHost} for route {route}");
                            }
                        }
                    }
                }
            }
            if (hasBindleDefinitions)
            {
                foreach (var bindle in options.Bindles)
                {
                    var route = bindle.Key;
                    if (route.Contains("{", StringComparison.InvariantCulture) && route.Contains("}", StringComparison.InvariantCulture))
                    {
                        result.AppendLine($"Bindle route '{route}' cannot contain either {{ or }} {route}");
                    }

                    if (hasModuleDefinitions && options.Modules.ContainsKey(bindle.Key))
                    {
                        result.AppendLine($"Bindle route '{route}' is duplciate of Module route - routes must be unique");
                    }

                    if (string.IsNullOrEmpty(bindle.Value.Name))
                    {
                        result.AppendLine($"Bindle Name missing for {route} ");
                    }
                }
            }

            return result.Length == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(result.ToString());
        }
    }
}
