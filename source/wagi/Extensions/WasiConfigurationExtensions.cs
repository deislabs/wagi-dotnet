namespace Deislabs.WAGI.Extensions
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using Microsoft.Extensions.Logging;
  using Wasmtime;

  /// <summary>
  /// Extension methods for WasiConfiguration.
  /// </summary>
  public static class WasiConfigurationExtensions
  {
    /// <summary>
    /// Adds volumes to WasiConfiguration as preopened directories.
    /// </summary>
    /// <param name="config">The WasiConfiguration.</param>
    /// <param name="volumes">The volumes to add.</param>
    /// <param name="logger">ILogger implementation.</param>
    /// <returns>WasiConfiguration.</returns>
    public static WasiConfiguration WithVolumes(this WasiConfiguration config, IDictionary<string, string> volumes, ILogger logger)
    {
      volumes ??= new Dictionary<string, string>();
      foreach (var volume in volumes)
      {
        // TODO check for valid guest path
        // File path is not user provided input
#pragma warning disable CA3003
        if (!Directory.Exists(volume.Value))
#pragma warning restore CA3003
        {
          logger.LogError($"Error opening Volume. {volume.Value} mapped to {volume.Key} does not exist");
        }
#pragma warning disable CA1062
        config = config.WithPreopenedDirectory(volume.Value, volume.Key);
#pragma warning restore CA1062
      }

      return config;
    }

    /// <summary>
    /// Adds environment variables to WasiConfiguration.
    /// </summary>
    /// <param name="config">The WasiConfiguration.</param>
    /// <param name="environment">The environment variables to add.</param>
    /// <param name="logger">ILogger implementation.</param>
    /// <returns>WasiConfiguration.</returns>
    public static WasiConfiguration WithEnvironment(this WasiConfiguration config, IDictionary<string, string> environment, ILogger logger)
    {
      environment ??= new Dictionary<string, string>();
      foreach (var envvar in environment)
      {
        try
        {
#pragma warning disable CA1062
          config = config.WithEnvironmentVariable(envvar.Key, envvar.Value);
#pragma warning restore CA1062
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
          logger.LogError($"Failed to add environment variable {envvar.Key} Exception: {ex}");
        }
      }

      return config;
    }
  }
}
