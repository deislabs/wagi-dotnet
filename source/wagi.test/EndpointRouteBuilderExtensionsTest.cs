
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Deislabs.Wagi.Test.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Deislabs.Wagi.Test
{

    public class EndpointRouteBuilderExtensionsTest
    {

        [Fact]
        public async Task Test_Handles_No_Configuration_As_Log_Warning()
        {
            var (mockLogger, mockLoggerFactory) = CreateMockLoggerAndFactory();
            var testServer = TestHelpers.CreateTestServer(new string[] { "testdata/appsettingsNoConfig.json" }, mockLoggerFactory);
            await testServer.Host.StartAsync();
            mockLogger.VerifyLogWarning("No modules found in configuration.");
            await testServer.Host.StopAsync();
        }

        [Fact]
        public async Task Test_Handles_Configuration_With_Empty_Config()
        {
            var (mockLogger, mockLoggerFactory) = CreateMockLoggerAndFactory();
            var testServer = TestHelpers.CreateTestServer(new string[] { "testdata/appsettingsEmptyConfig.json" }, mockLoggerFactory);
            await testServer.Host.StartAsync();
            mockLogger.VerifyLogWarning("No modules found in configuration.");
            await testServer.Host.StopAsync();
        }

        [Fact]
        public async Task Test_Handles_Configuration_With_WAT_Module()
        {
            var (mockLogger, mockLoggerFactory) = CreateMockLoggerAndFactory();
            var testServer = TestHelpers.CreateTestServer(new string[] { "testdata/appsettingsWATConfig.json" }, mockLoggerFactory);
            await testServer.Host.StartAsync();
            mockLogger.VerifyLogTrace($"Adding Route Endpoint for Module: hellowat File: testdata/modules{Path.DirectorySeparatorChar}hello.wat Entrypoint: Default Route:/hellowat Hostnames: ");
            await testServer.Host.StopAsync();
        }

        [Fact]
        public async Task Test_Handles_Configuration_With_WAGI_Module()
        {
            var (mockLogger, mockLoggerFactory) = CreateMockLoggerAndFactory();
            var testServer = TestHelpers.CreateTestServer(new string[] { "testdata/appsettingsWAGIConfig.json" }, mockLoggerFactory);
            await testServer.Host.StartAsync();
            mockLogger.VerifyLogTrace($"Adding Route Endpoint for Module: fibonacci File: testdata/modules{Path.DirectorySeparatorChar}fibonacci.wasm Entrypoint: Default Route:/fibonacci Hostnames: ");
            await testServer.Host.StopAsync();
        }

        [Fact]
        public void Test_Module_Is_Missing()
        {
            var (mockLogger, mockLoggerFactory) = CreateMockLoggerAndFactory();
            var testServer = TestHelpers.CreateTestServer(new string[] { "testdata/appsettingsModuleIsMissingConfig.json" }, mockLoggerFactory, context => new StartupTest(context.Configuration, typeof(OptionsValidationException), $"Module file testdata/modules{Path.DirectorySeparatorChar}dontexist.wasm not found for module name dontexist{Environment.NewLine}"));
            testServer.CreateClient();
        }

        [Fact]
        public void Test_Route_Is_Missing()
        {
            var (mockLogger, mockLoggerFactory) = CreateMockLoggerAndFactory();
            var testServer = TestHelpers.CreateTestServer(new string[] { "testdata/appsettingsRouteIsMissingConfig.json" }, mockLoggerFactory, context => new StartupTest(context.Configuration, typeof(OptionsValidationException), $"Route should not be null or empty for module name noroute{Environment.NewLine}"));
            testServer.CreateClient();
        }

        [Fact]
        public async Task Test_Handles_Configuration_With_Custom_Section_Name()
        {
            var (mockLogger, mockLoggerFactory) = CreateMockLoggerAndFactory();
            var testServer = TestHelpers.CreateTestServer(new string[] { "testdata/appsettingsCustomSectionNameConfig.json" }, mockLoggerFactory, context => new StartupTest(context.Configuration, "custom"));
            await testServer.Host.StartAsync();
            mockLogger.VerifyLogTrace($"Adding Route Endpoint for Module: hellowat File: testdata/modules{Path.DirectorySeparatorChar}hello.wat Entrypoint: Default Route:/hellowat Hostnames: ");
            await testServer.Host.StopAsync();
        }

        [Fact]
        public void Test_Handles_Modules_Directory_Does_Not_Exist()
        {
            var (mockLogger, mockLoggerFactory) = CreateMockLoggerAndFactory();
            var testServer = TestHelpers.CreateTestServer(new string[] { "testdata/appsettingsModulesDirectoryDoesNotExistsConfig.json" }, mockLoggerFactory, context => new StartupTest(context.Configuration, typeof(OptionsValidationException), $"Module Path not found dontexist{Environment.NewLine}Module file dontexist{Path.DirectorySeparatorChar}fibonacci.wasm not found for module name fibonacci{Environment.NewLine}"));
            testServer.CreateClient();
        }

        [Fact]
        public async Task Test_Handles_Configuration_With_Modules_Toml()
        {
            var (mockLogger, mockLoggerFactory) = CreateMockLoggerAndFactory();
            var testServer = TestHelpers.CreateTestServer(new string[] { "testdata/appsettings.json", "testdata/modules.toml" }, mockLoggerFactory, context => new StartupTest(context.Configuration, "wagi"));
            await testServer.Host.StartAsync();

            var expectedLogMessage = new StringBuilder();
            mockLogger.VerifyLogTrace("Adding Route Endpoint for Module: static File: testdata/modules/fileserver/fileserver.gr.wasm Entrypoint: Default Route:/static/{**path} Hostnames: ");
            mockLogger.VerifyLogTrace("Mapped Wildcard Route: /static/... to /static/{**path}");
            await testServer.Host.StopAsync();
        }

        [Fact]
        public async Task Test_Handles_Configuration_With_Multiple_Modules_Toml()
        {
            var (mockLogger, mockLoggerFactory) = CreateMockLoggerAndFactory();
            var testServer = TestHelpers.CreateTestServer(new string[] { "testdata/appsettings.json", "testdata/multipleModules.toml" }, mockLoggerFactory, context => new StartupTest(context.Configuration, "wagi"));
            await testServer.Host.StartAsync();
            var expectedLogMessage = new StringBuilder();
            mockLogger.VerifyLogTrace("Adding Route Endpoint for Module: static File: testdata/modules/fileserver/fileserver.gr.wasm Entrypoint: Default Route:/static/{**path} Hostnames: ");
            mockLogger.VerifyLogTrace("Adding Route Endpoint for Module: static1 File: testdata/modules/fileserver/fileserver.gr.wasm Entrypoint: Default Route:/static1/{**path} Hostnames: ");
            mockLogger.VerifyLogTrace("Adding Route Endpoint for Module: static2 File: testdata/modules/fileserver/fileserver.gr.wasm Entrypoint: Default Route:/static2/{**path} Hostnames: ");
            mockLogger.VerifyLogTrace("Mapped Wildcard Route: /static/... to /static/{**path}");
            mockLogger.VerifyLogTrace("Mapped Wildcard Route: /static1/... to /static1/{**path}");
            mockLogger.VerifyLogTrace("Mapped Wildcard Route: /static2/... to /static2/{**path}");
            await testServer.Host.StopAsync();
        }

        private static (Mock<ILogger>, Mock<ILoggerFactory>) CreateMockLoggerAndFactory()
        {
            var mockLogger = new Mock<ILogger>();
            mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
            return (mockLogger, mockLoggerFactory);
        }

    }
}
