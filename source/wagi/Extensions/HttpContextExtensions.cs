namespace Deislabs.WAGI.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Deislabs.WAGI.Helpers;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// HttpContextExtension for running WAGI Requests.
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Runs a WAGI Request.
        /// </summary>
        /// <param name="context">HttpContext for the request.</param>
        /// <param name="wasmFile">The WASM File name.</param>
        /// <param name="httpClientFactory">The IHttpClientFactory.</param>
        /// <param name="entryPoint">The WASM Module Entrypoint.</param>
        /// <param name="moduleResolver">Module resolver to get wasmtime Module and Engine.</param>
        /// <param name="volumes">The volumes to be added to the WasiConfiguration as preopened directories.</param>
        /// <param name="environment">The environment variables to be added to the WasiConfiguration.</param>
        /// <param name="allowedHosts">The hosts that the module is allowed to connect to.</param>
        /// <param name="maxHttpRequests">The maximum number of HTTP Requests that the module can make.</param>
        public static async Task RunWAGIRequest(this HttpContext context, string wasmFile, IHttpClientFactory httpClientFactory, string entryPoint, IModuleResolver moduleResolver, IDictionary<string, string> volumes, IDictionary<string, string> environment, List<Uri> allowedHosts, int maxHttpRequests)
        {
#pragma warning disable CA1062
            var wagiHost = new WAGIHost(context, httpClientFactory, entryPoint, wasmFile, moduleResolver, volumes, environment, allowedHosts, maxHttpRequests);
#pragma warning restore CA1062
            await wagiHost.ProcessRequest();
        }
    }
}
