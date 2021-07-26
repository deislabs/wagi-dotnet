namespace Deislabs.Wagi.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using Deislabs.Wagi.Configuration;
    using Deislabs.Wagi.DataSource;
    using Deislabs.Wagi.Helpers;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Wasi.Experimental.Http;
    using Wasmtime;

    /// <summary>
    /// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to add routes.
    /// </summary>
    public static class EndpointRouteBuilderExtensions
    {

        /// <summary>
        /// Adds a route endpoint to the <see cref="IEndpointRouteBuilder"/> for each WAGI Function defined in configuration.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> instance being extended. </param>
        /// <returns>IEndpointConventionBuilder to configure endpoints.</returns>
        public static IEndpointConventionBuilder MapWagiModules(this IEndpointRouteBuilder endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            var dataSource = endpoints.ServiceProvider.GetRequiredService<WagiEndpointDataSource>();
            if (dataSource == null)
            {
                throw new InvalidOperationException("Unable to find the required services. Please add all the required services by calling 'IServiceCollection.AddWagi(...)' inside the call to 'ConfigureServices(...)' in the application startup code.");
            }

            endpoints.DataSources.Add(dataSource);
            return dataSource;
        }
    }
}
