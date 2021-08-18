
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Deislabs.Wagi.Configuration.Modules.Toml
{
    /// <summary>
    /// Extension methods for adding <see cref="ModulesTomlConfigurationProvider"/>.
    /// </summary>
    public static class ModulesTomlConfigurationExtensions
    {
        /// <summary>
        /// Adds a Wagi Modules.toml configuration source to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="path">Path relative to the base path stored in
        /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
        /// <param name="optional">Whether the file is optional.</param>
        /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddModulesTomlFile(this IConfigurationBuilder builder, string path, bool optional = true, bool reloadOnChange = false)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Invalid File Path", nameof(path));
            }

            return builder.AddModulesTomlFile(s =>
            {
                s.Path = path;
                s.Optional = optional;
                s.ReloadOnChange = reloadOnChange;
                s.ResolveFileProvider();
            });
        }

        /// <summary>
        /// Adds a Wagi Modules.toml configuration source to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="configureSource">Configures the source.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddModulesTomlFile(this IConfigurationBuilder builder, Action<ModulesTomlConfigurationSource> configureSource)
            => builder.Add(configureSource);

    }
}
