using System;
using Deislabs.WAGI.Configuration;
using Deislabs.WAGI.DataSource;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Deislabs.WAGI.Extensions
{
    /// <summary>
    /// Provides extension methods for <see cref="IServiceCollection"/> to add manage configuration.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services required for using options.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="config">The <see cref="IConfiguration"/> for the application.</param>
        /// <param name="section">The configuration section containing the modules to be processed. </param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddWASM(this IServiceCollection services, IConfiguration config, string section = "WASM")
        {
            _ = config ?? throw new ArgumentException("config is null");
            services.Configure<WASMModules>(config.GetSection(section));
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<WASMModules>, ValidateConfiguration>());
            services.AddSingleton(f => f.GetRequiredService<IOptions<WASMModules>>().Value);
            services.AddSingleton<WagiEndpointDataSource>();
            return services;
        }
    }
}
