using System;
using Deislabs.Wagi.Configuration;
using Deislabs.Wagi.DataSource;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Deislabs.Wagi.Extensions
{
    /// <summary>
    /// Provides extension methods for <see cref="IServiceCollection"/> to add manage configuration.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the services required for using Wagi Modules.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="config">The <see cref="IConfiguration"/> for the application.</param>
        /// <param name="section">The configuration section containing the modules to be processed. </param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddWagi(this IServiceCollection services, IConfiguration config, string section = "Wagi")
        {
            _ = config ?? throw new ArgumentException("config is null");
            services.Configure<WagiModules>(config.GetSection(section));
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<WagiModules>, ValidateConfiguration>());
            services.AddSingleton(f => f.GetRequiredService<IOptions<WagiModules>>().Value);
            services.AddSingleton<WagiEndpointDataSource>();
            return services;
        }
    }
}
