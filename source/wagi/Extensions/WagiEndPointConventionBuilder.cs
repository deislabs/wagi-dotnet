namespace Deislabs.WAGI.Extensions
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Builder;

    /// <summary>
    /// WagiEndPointConventionBuilder implements IEndpointConventionBuilder for WASMModules.
    /// </summary>
    public sealed class WagiEndPointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly IList<IEndpointConventionBuilder> endpointConventionBuilders;

        /// <summary>
        ///  Initializes a new instance of the <see cref="WagiEndPointConventionBuilder"/> class.
        /// </summary>
        /// <param name="endpointConventionBuilders">List of IEndPointConventionBuilders.</param>
        public WagiEndPointConventionBuilder(IList<IEndpointConventionBuilder> endpointConventionBuilders)
        {
            this.endpointConventionBuilders = endpointConventionBuilders;
        }

        /// <summary>
        /// Adds the specified convention to the builder.
        /// </summary>
        /// <param name="convention">The convention to add.</param>
        public void Add(Action<EndpointBuilder> convention)
        {
            foreach (var endpointConventionBuilder in this.endpointConventionBuilders)
            {
                endpointConventionBuilder.Add(convention);
            }
        }
    }
}
