
using System;
using System.IO;
using System.Threading.Tasks;
using Deislabs.WAGI.Test;
using Deislabs.WAGI.Test.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Deislabs.WAGI.Extensions.Test
{

    public class EndpointRouteBuilderExtensionsTest
    {

        [Fact]
        public async Task Test_Handles_No_Configuration_As_Log_Error()
        {
            var mockLogger = new Mock<ILogger>();

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
            var testServer = EndpointRouteBuilderExtensionsTest.CreateTestServer("testdata/appsettingsNoConfig.json", mockLoggerFactory);
            await testServer.Host.StartAsync();
            mockLogger.VerifyLogError("No configuration found in section WASM");
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
            mockLogger.VerifyLogError("No Module configuration found in section WASM");
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
            mockLogger.VerifyLogTrace($"Added Route Endpoint for Route: /hellowat File: testdata/modules{Path.DirectorySeparatorChar}hello.wat Entrypoint: Default");
            await testServer.Host.StopAsync();
        }

        [Fact]
        public async Task Test_Handles_Configuration_With_WASM_Module()
        {
            var mockLogger = new Mock<ILogger>();

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
            var testServer = EndpointRouteBuilderExtensionsTest.CreateTestServer("testdata/appsettingsWASMConfig.json", mockLoggerFactory);
            await testServer.Host.StartAsync();
            mockLogger.VerifyLogTrace($"Added Route Endpoint for Route: /fibonacci File: testdata/modules{Path.DirectorySeparatorChar}fibonacci.wasm Entrypoint: Default");
            await testServer.Host.StopAsync();
        }

        [Fact]
        public async Task Test_Module_Is_Missing()
        {
            var mockLogger = new Mock<ILogger>();

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
            var testServer = EndpointRouteBuilderExtensionsTest.CreateTestServer("testdata/appsettingsModuleIsMissingConfig.json", mockLoggerFactory);
            await testServer.Host.StartAsync();
            mockLogger.VerifyLogError($"Module file testdata/modules{Path.DirectorySeparatorChar}dontexist.wasm not found for route /dontexist - skipping");
            await testServer.Host.StopAsync();
        }

        [Fact]
        public async Task Test_Handles_Configuration_With_Custom_Section_Name()
        {
            var mockLogger = new Mock<ILogger>();

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
            var testServer = EndpointRouteBuilderExtensionsTest.CreateTestServer("testdata/appsettingsCustomSectionNameConfig.json", mockLoggerFactory, context => new StartupTest("custom"));
            await testServer.Host.StartAsync();
            mockLogger.VerifyLogTrace($"Added Route Endpoint for Route: /hellowat File: testdata/modules{Path.DirectorySeparatorChar}hello.wat Entrypoint: Default");
            await testServer.Host.StopAsync();
        }

        [Fact]
        public void Test_Handles_Modules_Directory_Does_Not_Exist()
        {
            var mockLogger = new Mock<ILogger>();

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
            var testServer = EndpointRouteBuilderExtensionsTest.CreateTestServer("testdata/appsettingsModulesDirectoryDoesNotExistsConfig.json", mockLoggerFactory, context => new StartupTest(typeof(ApplicationException), "Module Path not found modules"));
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
                          return new StartupTest();
                      }
                      return startUpTestFactory(context);
                  }));
    }
}
