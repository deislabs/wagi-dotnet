
using Microsoft.AspNetCore.TestHost;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Deislabs.WAGI.Test;
using Deislabs.WAGI.Test.Extensions;
using Moq;
using Microsoft.AspNetCore.Hosting;

namespace Deislabs.WAGI.Extensions.Test
{

  public class EndpointRouteBuilderExtensionsTest
  {

    [Fact]
    public void Test_Handles_No_Configuration_As_Log_Error()
    {
      var mockLogger = new Mock<ILogger>();

      var mockLoggerFactory = new Mock<ILoggerFactory>();
      mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
      var testServer = new TestServer(new WebHostBuilder()
               .ConfigureAppConfiguration((context, builder) =>
               {
                 builder.AddJsonFile("testdata/appsettingsNoConfig.json", false);
               })
               .ConfigureServices(services =>
                {
                  services.AddSingleton<ILoggerFactory>(mockLoggerFactory.Object);
                })
               .UseStartup<StartupTest>());

      testServer.CreateClient();
      mockLogger.VerifyLogError("No configuration found in section WASM");

    }

  }
}
