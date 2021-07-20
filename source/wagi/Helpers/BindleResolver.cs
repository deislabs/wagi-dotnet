using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Deislabs.Bindle;
using Deislabs.WAGI.Configuration;
using Microsoft.Extensions.Logging;

namespace Deislabs.WAGI.Helpers
{
    /// <summary>
    /// Provides support for resolving a bindle URL into a module confguration
    /// </summary>
    public class BindleResolver
    {
        private readonly WASMModules wasmModules;
        private readonly ILogger logger;

        private static string CachePath => Path.Combine(Directory.GetCurrentDirectory(), "bindlecache");

        /// <summary>
        /// Initializes a new instance of the <see cref="BindleResolver"/> class.
        /// </summary>
        /// <param name="wasmModules">The WASM Module configuration</param>
        /// <param name="loggerFactory">LoggerFactory for creating ILogger. </param>
        public BindleResolver(WASMModules wasmModules, ILoggerFactory loggerFactory)
        {
            this.wasmModules = wasmModules;
            this.logger = loggerFactory?.CreateLogger(typeof(BindleResolver).FullName);
            this.wasmModules.Modules ??= new();
        }

        /// <summary>
        /// Downloads Bindles and their Assets and updates WASM Module info.
        /// </summary>
        public async Task LoadInvoice()
        {
            foreach (var bindle in this.wasmModules.Bindles)
            {
                var bindleInfo = bindle.Value;
                var routePrefix = bindle.Key;
                logger.LogTrace($"Processing Bindle {bindleInfo.Name} from Server {this.wasmModules.BindleServer} with route prefix '{routePrefix}'.");
                var bindleClient = new BindleClient(this.wasmModules.BindleServer);
                var invoice = await bindleClient.GetInvoice(bindleInfo.Name);
                var parcels = invoice.Parcels.Where(p => p.Label.MediaType == "application/wasm" && p.Conditions.MemberOf.Count == 0);
                foreach (var parcel in parcels)
                {
                    logger.LogTrace($"Processing Parcel {parcel.Label.Name} with SHA256 {parcel.Label.Sha256}.");
                    var modulePath = GetAssetCacheDirectory(this.wasmModules.ModulePath, parcel.Label.Sha256);
                    var moduleInfo = await GetModuleInfo(parcel, modulePath, bindleInfo.Environment, bindleInfo.Name, bindleClient);
                    var routeSuffix = parcel.Label.Feature.ContainsKey("Route") ? parcel.Label.Feature["Route"].Values.FirstOrDefault() : "/";
                    logger.LogTrace($"Creating route with prefix '{routePrefix}' and suffix '{routeSuffix}'.");
                    var route = routeSuffix == "/" ? routePrefix : Path.Join(routePrefix, routeSuffix);
                    try
                    {
                        this.wasmModules.Modules.Add(route, moduleInfo);
                    }
                    catch (ArgumentException ex)
                    {
                        logger.LogError($"Attempt to add route Failed. : {ex}");
                        logger.LogError($"Skipping loading {route} for bindle {bindleInfo.Name} from server {this.wasmModules.BindleServer}.");
                    }

                    foreach (var group in parcel.Conditions.Requires)
                    {
                        logger.LogTrace($"Processing Group {group}.");
                        var assetCachePath = GetAssetCacheDirectory(modulePath, bindleInfo.Name);
                        var members = invoice.Parcels.Where(p => p.Conditions.MemberOf.Contains(group));
                        foreach (var member in members)
                        {
                            logger.LogTrace($"Processing Member {member.Label.Name}.");
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

        private async Task<WASMModuleInfo> GetModuleInfo(Parcel parcel, string modulePath, Dictionary<string, string> env, string bindleName, BindleClient bindleClient)
        {
            var label = parcel.Label;
            await GetParcelAsset(modulePath, label.Sha256, label.Name, bindleName, bindleClient);
            return new()
            {
                FileName = Path.Join(label.Sha256, label.Name),
                Entrypoint = label.Feature.ContainsKey("entrypoint") ? label.Feature["entrypoint"].Values.FirstOrDefault() : null,
                AllowedHosts = label.Feature.ContainsKey("allowed_hosts") ? new Collection<string>(label.Feature["allowed_hosts"]?.Values.ToList<string>()) : new Collection<string>(),
                Environment = env
            };
        }
        private string GetAssetCacheDirectory(string basePath, string directory)
        {
            var path = Path.Join(basePath, directory);
            Directory.CreateDirectory(path);
            logger.LogTrace($"Creating Directory {path}.");
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
                throw new ApplicationException($"Attempt to traverse file system with path {name}");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(parcelFileModulePath));
            if (!File.Exists(parcelFileCachePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(parcelFileCachePath));
                var parcelBytes = await GetParcel(bindleName, parcelId, bindleClient);
                logger.LogTrace($"Writing File {parcelFileCachePath}.");
                await File.WriteAllBytesAsync(parcelFileCachePath, parcelBytes);
            }
            // The file should not ever exist but it does not matter if it is overwritten.
            // This may need revising when dynamic updates are supported.
            logger.LogTrace($"Copying Cached File {parcelFileCachePath} to {parcelFileModulePath}.");
            File.Copy(parcelFileCachePath, parcelFileModulePath, true);
        }

        private async Task<byte[]> GetParcel(string invoiceId, string parcelId, BindleClient bindleClient)
        {
            logger.LogTrace($"Downloading Parcel with InoviceId {invoiceId} ParcelId {parcelId}.");
            using var content = await bindleClient.GetParcel(invoiceId, parcelId);
            return await content.ReadAsByteArrayAsync();
        }
    }
}
