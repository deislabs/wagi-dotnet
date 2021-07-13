#pragma warning disable CA2227
using System;
using System.Collections.Generic;

namespace Deislabs.WAGI.Configuration
{
    /// <summary>
    /// BindleInfo contains information for modules defined as Bindles.
    /// </summary>
    public class BindleInfo
    {
        /// <summary>
        /// Gets or sets the Bindle Server URL to get the bindle from
        /// </summary>
        public Uri BindleUrl { get; set; }

        /// <summary>
        /// Gets or sets a Bindle Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets any environment variables to be set for the Bindle
        /// </summary>
        public Dictionary<string, string> Environment { get; set; }
    }
}
#pragma warning restore CA2227
