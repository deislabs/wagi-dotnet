using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Deislabs.Bindle;
using Deislabs.Wagi.Configuration;
using Deislabs.Wagi.Extensions;
using Microsoft.Extensions.Logging;

namespace Deislabs.Wagi.Helpers
{
    /// <summary>
    /// Provides support for resolving a bindle URL into a module confguration
    /// </summary>
    public class BindleResolver
    {
        private readonly WagiModules wagiModules;
        private readonly ILogger logger;

        private static string CachePath => Path.Combine(Directory.GetCurrentDirectory(), "bindlecache");

        /// <summary>
        /// Initializes a new instance of the <see cref="BindleResolver"/> class.
        /// </summary>
        /// <param name="wagiModules">The WAGI Module configuration</param>
        /// <param name="loggerFactory">LoggerFactory for creating ILogger. </param>
        public BindleResolver(WagiModules wagiModules, ILoggerFactory loggerFactory)
        {
            this.wagiModules = wagiModules;
            this.logger = loggerFactory?.CreateLogger(typeof(BindleResolver).FullName);
            this.wagiModules.Modules ??= new();
        }

        /// <summary>
        /// Downloads Bindles and their Assets and updates WASM Module info.
        /// </summary>
        public async Task LoadInvoice()
        {
            foreach (var bindle in this.wagiModules.Bindles)
            {
                var bindleInfo = bindle.Value;
                var name = bindle.Key;
                var message = $"Processing Bindle {bindleInfo.Name} from Server {this.wagiModules.BindleServer} with name '{name}'.";
                logger.TraceMessage(message);
                var bindleClient = new BindleClient(this.wagiModules.BindleServer);
                var invoice = await bindleClient.GetInvoice(bindleInfo.Name);
                var parcels = invoice.Parcels.Where(p => p.Label.MediaType == "application/wasm" && p.Conditions.MemberOf.Count == 0);
                foreach (var parcel in parcels)
                {
                    logger.TraceMessage($"Processing Parcel {parcel.Label.Name} with SHA256 {parcel.Label.Sha256}.");
                    var modulePath = GetAssetCacheDirectory(this.wagiModules.ModulePath, parcel.Label.Sha256);
                    var routeSuffix = parcel.Label.Feature.ContainsKey("Route") ? parcel.Label.Feature["Route"].Values.FirstOrDefault() : "/";
                    var route = routeSuffix == "/" ? bindleInfo.Route : Path.Join(bindleInfo.Route, routeSuffix);
                    var moduleInfo = await GetModuleInfo(parcel, modulePath, bindleInfo.Environment, bindleInfo.Name, bindleClient, bindleInfo.Hostnames, route);
                    logger.TraceMessage($"Creating route with prefix '{bindleInfo.Route}' and suffix '{routeSuffix}'.");
                    try
                    {
                        this.wagiModules.Modules.Add(name, moduleInfo);
                    }
                    catch (ArgumentException ex)
                    {
                        logger.TraceMessage("Attempt to add module Failed", ex);
                        logger.TraceMessage($"Skipping loading {name} for bindle {bindleInfo.Name} from server {this.wagiModules.BindleServer}.");
                    }

                    foreach (var group in parcel.Conditions.Requires)
                    {
                        logger.TraceMessage($"Processing Group {group}.");
                        var assetCachePath = GetAssetCacheDirectory(modulePath, bindleInfo.Name);
                        var members = invoice.Parcels.Where(p => p.Conditions.MemberOf.Contains(group));
                        foreach (var member in members)
                        {
                            logger.TraceMessage($"Processing Member {member.Label.Name}.");
                            var file = member.Label.Feature["wagi"].Where(f => f.Key == "file").Select(kvp => kvp.Value).FirstOrDefault();
                            if (!string.IsNullOrEmpty(file))
                            {
                                await GetParcelAsset(assetCachePath, member.Label.Sha256, member.Label.Name, bindleInfo.Name, bindleClient);

                                if (!moduleInfo.Volumes.Any())
                                {
                                    moduleInfo.Volumes.Add("/", assetCachePath);
                                }
                            }
                        }

                    }

                }

            }

        }

        private async Task<WagiModuleInfo> GetModuleInfo(Parcel parcel, string modulePath, Dictionary<string, string> env, string bindleName, BindleClient bindleClient, Collection<string> hostnames, string route)
        {
            var label = parcel.Label;
            await GetParcelAsset(modulePath, label.Sha256, label.Name, bindleName, bindleClient);
            return new()
            {
                FileName = Path.Join(label.Sha256, label.Name),
                Entrypoint = label.Feature.ContainsKey("entrypoint") ? label.Feature["entrypoint"].Values.FirstOrDefault() : null,
                AllowedHosts = label.Feature.ContainsKey("allowed_hosts") ? new Collection<string>(label.Feature["allowed_hosts"]?.Values.ToList<string>()) : new Collection<string>(),
                Environment = env,
                Hostnames = hostnames,
                Route = route
            };
        }
        private string GetAssetCacheDirectory(string basePath, string directory)
        {
            var path = Path.Join(basePath, directory);
            Directory.CreateDirectory(path);
            logger.CreatedDirectory(path);
            return path;
        }

        private async Task GetParcelAsset(string assetPath, string parcelId, string name, string bindleName, BindleClient bindleClient)
        {
            var parcelFileCachePath = Path.Join(CachePath, parcelId);
            var parcelFileModulePath = Path.Join(assetPath, name);
            var rootPath = Path.GetFullPath(assetPath);
            var parcelFullPath = Path.GetFullPath(parcelFileModulePath);
            if (!parcelFullPath.StartsWith(rootPath, true, CultureInfo.InvariantCulture))
            {
                throw new InvalidOperationException($"Attempt to traverse file system with path {name}");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(parcelFileModulePath));
            if (!File.Exists(parcelFileCachePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(parcelFileCachePath));
                var parcelBytes = await GetParcel(bindleName, parcelId, bindleClient);
                logger.TraceMessage($"Writing File {parcelFileCachePath}.");
                await File.WriteAllBytesAsync(parcelFileCachePath, parcelBytes);
            }
            // The file should not ever exist but it does not matter if it is overwritten.
            // This may need revising when dynamic updates are supported.
            // TODO dont copy the file if it already exists and is not changed (it should be immutable)
            logger.TraceMessage($"Copying Cached File {parcelFileCachePath} to {parcelFileModulePath}.");
            File.Copy(parcelFileCachePath, parcelFileModulePath, true);
        }

        private async Task<byte[]> GetParcel(string invoiceId, string parcelId, BindleClient bindleClient)
        {
            logger.DownloadingParcel(invoiceId, parcelId);
            using var content = await bindleClient.GetParcel(invoiceId, parcelId);
            return await content.ReadAsByteArrayAsync();
        }
    }
}
