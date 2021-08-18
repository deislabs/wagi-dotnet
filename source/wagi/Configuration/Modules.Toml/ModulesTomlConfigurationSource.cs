using Microsoft.Extensions.Configuration;

namespace Deislabs.Wagi.Configuration.Modules.Toml
{

    /// <summary>
    /// Represents an Wagi Modules.toml file as an <see cref="IConfigurationSource"/>.
    /// </summary>
    public class ModulesTomlConfigurationSource : FileConfigurationSource
    {
        /// <summary>
        /// Builds the <see cref="ModulesTomlConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>An <see cref="ModulesTomlConfigurationProvider"/></returns>
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            EnsureDefaults(builder);
            return new ModulesTomlConfigurationProvider(this);
        }
    }
}
