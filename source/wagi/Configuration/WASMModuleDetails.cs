#pragma warning disable CA2227
namespace Deislabs.WAGI.Configuration
{
  using System.Collections.Generic;

  /// <summary>
  /// This class contains configuration for properties for an exposed WASM function.
  /// </summary>
  public class WASMModuleDetails
  {
    /// <summary>
    /// Gets or Sets the Module filename.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Gets or Sets the Function Name, if unset defaults to _start.
    /// </summary>
    public string Entrypoint { get; set; }

    /// <summary>
    /// Gets or Sets any volumes to be given to the WASM function.
    /// </summary>
    public Dictionary<string, string> Volumes { get; set; }

    /// <summary>
    /// Gets or Sets any environment variables to be set for the WASM function.
    /// </summary>
    public Dictionary<string, string> Environment { get; set; }

    /// <summary>
    /// Gets or Sets the allowed HTTP Method for this function.
    /// </summary>
    public string HttpMethod { get; set; }
  }
}
#pragma warning restore CA2227
