using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Options;
using Wasi.Experimental.Http;

namespace Deislabs.Wagi.Configuration
{
    /// <summary>
    /// Validates configuration for Wagi dotnet
    /// </summary>
    public class ValidateConfiguration : IValidateOptions<WagiModules>
    {
        private Dictionary<string, List<string>> routeToHosts;
        private List<string> paths;

        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string name, WagiModules options)
        {
            _ = options ?? throw new ArgumentException("Options should not be Null");
            this.routeToHosts = new();
            this.paths = new();
            StringBuilder result = new();
            var hasModuleDefinitions = options.Modules?.Any() ?? default;
            var hasBindleDefinitions = options.Bindles?.Any() ?? default;
            if (string.IsNullOrEmpty(options.ModulePath))
            {
                options.ModulePath = "modules";
            }

            if (hasModuleDefinitions && !Directory.Exists(options.ModulePath))
            {
                result.AppendLine(FormattableString.Invariant($"Module Path not found {options.ModulePath}"));
            }

            if (!string.IsNullOrEmpty(options.CacheConfigPath) && !File.Exists(options.CacheConfigPath))
            {
                result.AppendLine(FormattableString.Invariant($"Wasmtime cache config file {options.CacheConfigPath} does not exist"));
            }

            if (!string.IsNullOrEmpty(options.BindleServer) && !Uri.TryCreate(options.BindleServer, UriKind.Absolute, out _))
            {
                result.AppendLine(FormattableString.Invariant($"Bindle Server is Invalid"));
            }

            if (string.IsNullOrEmpty(options.BindleServer) && hasBindleDefinitions)
            {
                result.AppendLine(FormattableString.Invariant($"Bindle Server is not configured but there are bindle definitions"));
            }

            if (options.MaxHttpRequests > HttpRequestHandler.MaxHttpRequestLimit)
            {
                result.AppendLine(FormattableString.Invariant($"MaxHttpRequests of {options.MaxHttpRequests} not allowed - maximum is {HttpRequestHandler.MaxHttpRequestLimit}"));
            }

            if (hasModuleDefinitions)
            {
                foreach (var module in options.Modules)
                {
                    var moduleName = module.Key;
                    if (module.Value is null)
                    {
                        result.AppendLine(FormattableString.Invariant($"Missing module details for module name {moduleName}"));
                    }

                    var route = module.Value.Route;
                    if (string.IsNullOrEmpty(route))
                    {
                        result.AppendLine(FormattableString.Invariant($"Route should not be null or empty for module name {moduleName}"));
                        return ValidateOptionsResult.Fail(result.ToString());
                    }

                    if (route.Contains('{', StringComparison.InvariantCulture) && route.Contains('}', StringComparison.InvariantCulture))
                    {
                        result.AppendLine(FormattableString.Invariant($"Route '{route}' cannot contain either {{ or }} - module name {moduleName}"));
                    }

                    if (string.IsNullOrEmpty(module.Value.FileName))
                    {
                        result.AppendLine(FormattableString.Invariant($"Missing module file name for module name {moduleName}"));
                    }

                    var moduleType = module.Value.FileName.Split('.')[^1].ToUpperInvariant();
                    if (moduleType != "WAT" && moduleType != "WASM")
                    {
                        result.AppendLine(FormattableString.Invariant($"Module Filename extension should be either .wat or .wasm Filename: {module.Value.FileName} for module name {moduleName}"));
                    }

                    var moduleFileAndPath = Path.Join(options.ModulePath, module.Value.FileName);
                    if (!File.Exists(moduleFileAndPath))
                    {
                        result.AppendLine(FormattableString.Invariant($"Module file {moduleFileAndPath} not found for module name {moduleName}"));
                    }

                    if (!string.IsNullOrEmpty(module.Value.HttpMethod) && module.Value.HttpMethod.ToUpperInvariant() != "GET" && module.Value.HttpMethod.ToUpperInvariant() != "POST")
                    {
                        result.AppendLine(FormattableString.Invariant($"Module HttpMethod should be either GET or POST for module name {moduleName}"));
                    }

                    if (module.Value.AllowedHosts?.Count > 0)
                    {
                        foreach (var allowedHost in module.Value.AllowedHosts)
                        {
                            if (!Uri.TryCreate(allowedHost, UriKind.Absolute, out var _))
                            {
                                result.AppendLine(FormattableString.Invariant($"Invalid Uri for allowed host {allowedHost} for module name {moduleName}"));
                            }
                        }
                    }

                    if (module.Value.Hostnames?.Count > 0)
                    {
                        foreach (var hostName in module.Value.Hostnames)
                        {
                            if (!Uri.TryCreate(hostName, UriKind.Absolute, out var _))
                            {
                                result.AppendLine(FormattableString.Invariant($"Invalid Uri for hostname {hostName} for module name {moduleName}"));
                            }
                        }
                    }

                    if (CheckIfMappingExists(route, module.Value.Hostnames))
                    {
                        var hostnames = module.Value?.Hostnames is null ? "*" : string.Join(",", module.Value.Hostnames);
                        result.AppendLine(FormattableString.Invariant($"Attempt to associate Route '{route}' with hostnames '{hostnames}' for Module name '{moduleName}' failed, route is already mapped to one or more hosts"));
                    }
                }
            }

            if (hasBindleDefinitions)
            {
                foreach (var bindle in options.Bindles)
                {
                    var bindleName = bindle.Key;
                    var route = bindle.Value.Route;
                    if (string.IsNullOrEmpty(route))
                    {
                        result.AppendLine(FormattableString.Invariant($"Route should not be null or empty for bindle {bindleName}"));
                        return ValidateOptionsResult.Fail(result.ToString());
                    }

                    if (route.Contains('{', StringComparison.InvariantCulture) && route.Contains('}', StringComparison.InvariantCulture))
                    {
                        result.AppendLine(FormattableString.Invariant($"Bindle route '{route}' cannot contain either {{ or }} for bindle {bindleName}"));
                    }

                    if (hasModuleDefinitions && options.Modules.ContainsKey(bindle.Key))
                    {
                        result.AppendLine(FormattableString.Invariant($"Bindle '{bindleName}' is a duplciate of a Module name - names must be unique"));
                    }

                    if (string.IsNullOrEmpty(bindle.Value.Name))
                    {
                        result.AppendLine(FormattableString.Invariant($"Bindle Name missing for  bindle {bindleName} "));
                    }

                    if (CheckIfMappingExists(route, bindle.Value.Hostnames))
                    {
                        var hostnames = bindle.Value?.Hostnames is null ? "*" : string.Join(",", bindle.Value.Hostnames);
                        result.AppendLine(FormattableString.Invariant($"Attempt to associate Route '{route}' with hostnames '{hostnames}' for Bindle name '{bindleName}' failed, route is already mapped to one or more hosts"));
                    }
                }
            }

            return result.Length == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(result.ToString());
        }

        private bool CheckIfMappingExists(string route, ICollection<string> hostnames)
        {
            if (hostnames is null || hostnames.Count == 0)
            {
                return CheckIfMappingExists(route, "*");
            }
            foreach (var hostname in hostnames)
            {
                if (CheckIfMappingExists(route, hostname))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CheckIfMappingExists(string route, string hostname)
        {
            var routetoCheck = string.IsNullOrEmpty(route) ? "/" : route.TrimEnd('/');
            if (this.routeToHosts.TryGetValue(routetoCheck, out var hostnames))
            {
                if (hostnames.Contains(hostname) || hostnames.Contains("*"))
                {
                    return true;
                }

                hostnames.Add(hostname);
                this.routeToHosts[routetoCheck] = hostnames;
            }
            else
            {
                this.routeToHosts.Add(routetoCheck, new List<string> { hostname });
            }

            if (this.paths.Contains(routetoCheck))
            {
                if (hostname == "*")
                {
                    return true;
                }
            }
            else
            {
                this.paths.Add(routetoCheck);
            }

            return false;
        }
    }
}
