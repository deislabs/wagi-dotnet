using System;
#pragma warning disable CA2227
namespace Deislabs.WAGI.Configuration
{
    using System.Collections.Generic;

    /// <summary>
    /// This class contains details of the WASM modules and entrypoints to be exposed.
    /// </summary>
    public class WASMModules
    {
        /// <summary>
        /// Gets or sets path where the wasmtime cache configuration can be found.
        /// </summary>
        public string CacheConfigPath { get; set; }

        /// <summary>
        /// Gets or sets path where WASM Modules can be found.
        /// </summary>
        public string ModulePath { get; set; }

        /// <summary>
        ///  Gets or sets the maximum number of HTTP requests that modules can make - can be overridden per module by setting on WASMModuleDetails.
        /// </summary>
        public int MaxHttpRequests { get; set; }

        /// <summary>
        /// Gets or sets details of modules that can be executed via HTTP Requests, the key is the path to make the module available at.
        /// </summary>
        public Dictionary<string, WASMModuleInfo> Modules { get; set; }

        /// <summary>
        /// Gets or sets details of bindles that contain modules that can be executed via HTTP Requests, the key is the path prefix to make the modules in the bindle available at.
        /// </summary>
        public Dictionary<string, BindleInfo> Bindles { get; set; }
    }
}
#pragma warning restore CA2227
