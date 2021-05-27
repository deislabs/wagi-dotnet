#pragma warning disable CA2227
namespace Deislabs.WAGI.Configuration
{
  using System.Collections.Generic;
  using System.Collections.ObjectModel;

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

    /// <summary>
    ///  Gets or sets a value indicating whether this endpoint requires an authenitcated user.
    /// </summary>
    public bool Authorize { get; set; }

    /// <summary>
    ///  Gets or sets an array of Roles that the user must be a member of to access this endpoint.
    /// </summary>
    public Collection<string> Roles { get; set; }

    /// <summary>
    ///  Gets or sets an array of Polcies that the user must satisfy to access this endpoint.
    /// </summary>
    public Collection<string> Policies { get; set; }

    /// <summary>
    ///  Gets or sets an array of AllowedHosts that this module can comunicate with over HTTP.
    /// </summary>
    public Collection<string> AllowedHosts { get; set; }
  }
}
#pragma warning restore CA2227
