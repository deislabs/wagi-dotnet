#pragma warning disable CA2227
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Deislabs.WAGI.Configuration
{
    /// <summary>
    /// BindleInfo contains information for modules defined as Bindles.
    /// </summary>
    public class BindleInfo
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
        /// Gets or sets the Name of the Bindle
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets any environment variables to be set for the Bindle
        /// </summary>
        public Dictionary<string, string> Environment { get; set; }
    }
}
#pragma warning restore CA2227
