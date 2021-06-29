namespace Deislabs.WAGI.Extensions
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Builder;

    /// <summary>
    /// WAGIEndPointConventionBuilder implements IEndpointConventionBuilder for WASMModules.
    /// </summary>
    public sealed class WAGIEndPointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly IList<IEndpointConventionBuilder> endpointConventionBuilders;

        /// <summary>
        ///  Initializes a new instance of the <see cref="WAGIEndPointConventionBuilder"/> class.
        /// </summary>
        /// <param name="endpointConventionBuilders">List of IEndPointConventionBuilders.</param>
        public WAGIEndPointConventionBuilder(IList<IEndpointConventionBuilder> endpointConventionBuilders)
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
