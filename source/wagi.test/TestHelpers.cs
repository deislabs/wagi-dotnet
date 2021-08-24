using System;
using System.IO;
using Deislabs.Wagi.Configuration.Modules.Toml;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Deislabs.Wagi.Test
{
    class TestHelpers
    {
        protected internal static TestServer CreateTestServer(string[] filenames, Mock<ILoggerFactory> mockLoggerFactory, Func<WebHostBuilderContext, StartupTest> startUpTestFactory = null) => new(new WebHostBuilder()
          .ConfigureAppConfiguration((context, builder) =>
          {
              foreach (var filename in filenames)
              {
                  if (filename.EndsWith(".toml"))
                  {
                      builder.AddModulesTomlFile(filename, false);
                  }
                  else
                  {
                      builder.AddJsonFile(filename, false);
                  }
              }
          })
          .ConfigureServices(services =>
          {
              services.AddSingleton<ILoggerFactory>(mockLoggerFactory.Object);
          })
          .UseStartup<StartupTest>((context) =>
          {
              if (startUpTestFactory is null)
              {
                  return new StartupTest(context.Configuration);
              }
              return startUpTestFactory(context);
          }));

        protected internal static string GetEnvVarFromOuptut(string result, string envvarname)
        {
            string line;
            using (var reader = new StringReader(result))
            {
                while ((line = reader.ReadLine()) is not null)
                {
                    if (line.StartsWith(envvarname, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return line.Substring(line.IndexOf("=") + 1).Trim();
                    }
                }
            }
            return null;
        }
    }
}
