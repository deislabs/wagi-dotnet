using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Tommy;

namespace Deislabs.Wagi.Configuration.Modules.Toml
{
    /// <summary>
    /// A Wagi Modules.toml file <see cref="ConfigurationProvider"/>.
    /// </summary>
    public class ModulesTomlConfigurationProvider : FileConfigurationProvider
    {
        /// <summary>
        /// Initializes a new instance with the specified source.
        /// </summary>
        /// <param name="source">The source settings.</param>
        public ModulesTomlConfigurationProvider(ModulesTomlConfigurationSource source) : base(source) { }

        /// <summary>
        /// Read a stream of Wagi Modules.toml into a key/value dictionary.
        /// </summary>
        /// <param name="stream">The stream of TOML data.</param>
        /// <returns>The <see cref="IDictionary{String, String}"/> which was read from the stream.</returns>
        public static IDictionary<string, string> Read(Stream stream)
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var reader = new StreamReader(stream))
            {
                var toml = TOML.Parse(reader);
                foreach (TomlNode moduleNode in toml["module"])
                {
                    if (!moduleNode.TryGetNode("route", out var route))
                    {
                        throw new ArgumentNullException("route", "Route node in modules.toml table array is missing");
                    }

                    if (!route.IsString)
                    {
                        throw new InvalidDataException("Value for route should be a string");
                    }

                    if (!moduleNode.TryGetNode("module", out var module))
                    {
                        throw new ArgumentNullException("module", "Module node in modules.toml table array is missing");
                    }

                    if (!module.IsString)
                    {
                        throw new InvalidDataException("Value for module should be a string");
                    }

                    var moduleName = "root";
                    if (route.AsString.Value != "/")
                    {
                        moduleName = route.AsString.Value.TrimEnd('.').Trim('/');
                    }
                    var moduleKeyPrefix = $"Wagi:Modules:{moduleName}";

                    data[$"{moduleKeyPrefix}:route"] = route.AsString.Value;
                    data[$"{moduleKeyPrefix}:filename"] = module.AsString.Value;

                    if (moduleNode.TryGetNode("entrypoint", out var entrypoint))
                    {
                        if (!entrypoint.IsString)
                        {
                            throw new InvalidDataException("Value for entrypoint should be a string");
                        }
                        data[$"{moduleKeyPrefix}:entrypoint"] = entrypoint.AsString.Value;
                    }

                    if (moduleNode.TryGetNode("volumes", out var volumesNode))
                    {
                        if (volumesNode is TomlTable volumes)
                        {
                            var volumePrefix = $"{moduleKeyPrefix}:volumes";
                            foreach (var guestPath in volumes.Keys)
                            {
                                var hostPath = volumesNode[guestPath];
                                if (!hostPath.IsString)
                                {
                                    throw new InvalidDataException($"Value for volume {guestPath} should be a string");
                                }
                                data[$"{volumePrefix}:{guestPath}"] = hostPath.AsString.Value;
                            }
                        }
                        else
                        {
                            throw new InvalidDataException("Volumes node in modules.toml should be an in-line table");
                        }
                    }

                    if (moduleNode.TryGetNode("allowed_hosts", out var allowedHostsNode))
                    {
                        if (allowedHostsNode is TomlArray hosts)
                        {
                            var allowedHostsKey = $"{moduleKeyPrefix}:allowedHosts";
                            foreach (TomlNode hostNode in hosts)
                            {
                                if (!hostNode.IsString)
                                {
                                    throw new InvalidDataException($"Array value for allowedhosts should be strings");
                                }
                                data[allowedHostsKey] = hostNode.AsString.Value;
                            }
                        }
                        else
                        {
                            throw new InvalidDataException("Volumes node in modules.toml should be an array");
                        }
                    }

                    if (moduleNode.TryGetNode("http_max_concurrency", out var httpMaxConcurrencyNode))
                    {
                        if (!httpMaxConcurrencyNode.IsInteger)
                        {
                            throw new InvalidDataException("Value for http_max_concurrency should be an integer");
                        }
                        data[$"{moduleKeyPrefix}:maxhttprequests"] = entrypoint.AsString.Value;
                    }

                    if (moduleNode.TryGetNode("argv", out var argv))
                    {
                        if (!argv.IsString)
                        {
                            throw new InvalidDataException("Value for argv should be a string");
                        }
                        data[$"{moduleKeyPrefix}:argv"] = argv.AsString.Value;
                    }

                }

            }
            return data;
        }

        /// <summary>
        /// Loads Modules.toml configuration key/values from a stream into a provider.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to load Modules.toml configuration data from.</param>
        public override void Load(Stream stream)
        {
            Data = Read(stream);
        }
    }
}
