
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

namespace Deislabs.Wagi.Extensions.Test
{

    public class EndpointRouteBuilderExtensionsTest
    {

        [Fact]
        public async Task Test_Handles_No_Configuration_As_Log_Warning()
        {
            var mockLogger = new Mock<ILogger>();
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
            var testServer = EndpointRouteBuilderExtensionsTest.CreateTestServer("testdata/appsettingsNoConfig.json", mockLoggerFactory);
            await testServer.Host.StartAsync();
            mockLogger.VerifyLogWarning("No modules found in configuration.");
            await testServer.Host.StopAsync();
        }

        [Fact]
        public async Task Test_Handles_Configuration_With_Empty_Config()
        {
            var mockLogger = new Mock<ILogger>();
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
            var testServer = EndpointRouteBuilderExtensionsTest.CreateTestServer("testdata/appsettingsEmptyConfig.json", mockLoggerFactory);
            await testServer.Host.StartAsync();
            mockLogger.VerifyLogWarning("No modules found in configuration.");
            await testServer.Host.StopAsync();
        }

        [Fact]
        public async Task Test_Handles_Configuration_With_WAT_Module()
        {
            var mockLogger = new Mock<ILogger>();
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
            var testServer = EndpointRouteBuilderExtensionsTest.CreateTestServer("testdata/appsettingsWATConfig.json", mockLoggerFactory);
            await testServer.Host.StartAsync();
            mockLogger.VerifyLogTrace($"Adding Route Endpoint for Module: hellowat File: testdata/modules{Path.DirectorySeparatorChar}hello.wat Entrypoint: Default Route:/hellowat Hostnames: ");
            await testServer.Host.StopAsync();
        }

        [Fact]
        public async Task Test_Handles_Configuration_With_WAGI_Module()
        {
            var mockLogger = new Mock<ILogger>();
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
            var testServer = EndpointRouteBuilderExtensionsTest.CreateTestServer("testdata/appsettingsWAGIConfig.json", mockLoggerFactory);
            await testServer.Host.StartAsync();
            mockLogger.VerifyLogTrace($"Adding Route Endpoint for Module: fibonacci File: testdata/modules{Path.DirectorySeparatorChar}fibonacci.wasm Entrypoint: Default Route:/fibonacci Hostnames: ");
            await testServer.Host.StopAsync();
        }

        [Fact]
        public void Test_Module_Is_Missing()
        {
            var mockLogger = new Mock<ILogger>();
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
            var testServer = EndpointRouteBuilderExtensionsTest.CreateTestServer("testdata/appsettingsModuleIsMissingConfig.json", mockLoggerFactory, context => new StartupTest(context.Configuration, typeof(OptionsValidationException), $"Module file testdata/modules{Path.DirectorySeparatorChar}dontexist.wasm not found for module name dontexist{Environment.NewLine}"));
            testServer.CreateClient();
        }

        [Fact]
        public void Test_Route_Is_Missing()
        {
            var mockLogger = new Mock<ILogger>();
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
            var testServer = EndpointRouteBuilderExtensionsTest.CreateTestServer("testdata/appsettingsRouteIsMissingConfig.json", mockLoggerFactory, context => new StartupTest(context.Configuration, typeof(OptionsValidationException), $"Route should not be null or empty for module name noroute{Environment.NewLine}"));
            testServer.CreateClient();
        }

        [Fact]
        public async Task Test_Handles_Configuration_With_Custom_Section_Name()
        {
            var mockLogger = new Mock<ILogger>();
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
            var testServer = EndpointRouteBuilderExtensionsTest.CreateTestServer("testdata/appsettingsCustomSectionNameConfig.json", mockLoggerFactory, context => new StartupTest(context.Configuration, "custom"));
            await testServer.Host.StartAsync();
            mockLogger.VerifyLogTrace($"Adding Route Endpoint for Module: hellowat File: testdata/modules{Path.DirectorySeparatorChar}hello.wat Entrypoint: Default Route:/hellowat Hostnames: ");
            await testServer.Host.StopAsync();
        }

        [Fact]
        public void Test_Handles_Modules_Directory_Does_Not_Exist()
        {
            var mockLogger = new Mock<ILogger>();
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
            var testServer = EndpointRouteBuilderExtensionsTest.CreateTestServer("testdata/appsettingsModulesDirectoryDoesNotExistsConfig.json", mockLoggerFactory, context => new StartupTest(context.Configuration, typeof(OptionsValidationException), $"Module Path not found dontexist{Environment.NewLine}Module file dontexist{Path.DirectorySeparatorChar}fibonacci.wasm not found for module name fibonacci{Environment.NewLine}"));
            testServer.CreateClient();
        }

        public static TestServer CreateTestServer(string filename, Mock<ILoggerFactory> mockLoggerFactory, Func<WebHostBuilderContext, StartupTest> startUpTestFactory = null) => new(new WebHostBuilder()
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
    }
}
