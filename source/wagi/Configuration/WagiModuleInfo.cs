#pragma warning disable CA2227
namespace Deislabs.Wagi.Configuration
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// This class contains configuration for properties for an exposed WASM WAGI function.
    /// </summary>
    public class WagiModuleInfo
    {
        /// <summary>
        /// Gets or sets the route that is appended to the url of the server to form the endpoint URL
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// Gets or set the hostnames and ports to serve requests from , this can be used to constrain the endpoints that serve requests if this is not set then requests will be served on all endpoints, this can include the port but should not include the scheme.
        /// </summary>
        public Collection<string> Hostnames { get; set; }

        /// <summary>
        /// Gets or Sets the Module filename.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or Sets the Function Name, if unset defaults to _start.
        /// </summary>
        public string Entrypoint { get; set; }

        /// <summary>
        /// Gets or Sets the Arg value, if unset defaults to {SCIPT_NAME} ${ARGS}.
        /// This is used to override the value passed to a WASM module and is useful for passing in the script name and arguments 
        /// to modules that require specifically formatted values e.g. Ruby
        /// </summary>
        public string Argv { get; set; }

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

        /// <summary>
        ///  Gets or sets the maximum umber of HTTP requests that this module can make.
        /// </summary>
        public int MaxHttpRequests { get; set; }
    }
}
#pragma warning restore CA2227
