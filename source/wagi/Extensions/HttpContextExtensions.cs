namespace Deislabs.WAGI.Extensions
{
  using System.Collections.Generic;
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
    /// <param name="entryPoint">The WASM Module Entrypoint.</param>
    /// <param name="moduleType">Type of the module, can be either WASM or WAT.</param>
    /// <param name="volumes">The volumes to be added to the WasiConfiguration as preopened directories.</param>
    public static async Task RunWAGIRequest(this HttpContext context, string wasmFile, string entryPoint, string moduleType, IDictionary<string, string> volumes)
    {
#pragma warning disable CA1062
      var wagiHost = new WAGIHost(context, entryPoint, wasmFile, moduleType, volumes);
#pragma warning restore CA1062
      await wagiHost.ProcessRequest();
    }
  }
}
