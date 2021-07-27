using System;
using System.IO;
using System.Threading.Tasks;
using Deislabs.Wagi.Test;
using Deislabs.Wagi.Test.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Deislabs.Wagi.Test
{
    class TestHelpers
    {
        protected internal static TestServer CreateTestServer(string filename, Mock<ILoggerFactory> mockLoggerFactory, Func<WebHostBuilderContext, StartupTest> startUpTestFactory = null) => new(new WebHostBuilder()
          .ConfigureAppConfiguration((context, builder) =>
          {
              builder.AddJsonFile(filename, false);
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
                    if (line.StartsWith(envvarname))
                    {
                        return line.Split('=')[1]?.Trim() ?? null;
                    }
                }
            }
            return null;
        }
    }
}
