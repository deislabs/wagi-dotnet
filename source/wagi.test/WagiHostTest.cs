using System.Threading.Tasks;
using Castle.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Deislabs.Wagi.Test
{
    public class WagiHostTest
    {
        [Fact]
        public async Task Test_Path_Info()
        {
            (string path, string relativePath)[] tests =
            {
                ("/","/"),
                ("/path",""),
                ("/path/",""),
                ("/path/other","/other"),
                ("/path/other/","/other/"),
                ("/path/some/other","/some/other"),
                ("/some/other/path","/some/other/path"),
                ("/someotherroute","/someotherroute")
            };

            var mockLogger = new Mock<ILogger>();
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
            var testServer = TestHelpers.CreateTestServer(new string[] { "testdata/testPATH_INFOSettings.json" }, mockLoggerFactory);

            foreach (var (path, relativePath) in tests)
            {
                var client = testServer.CreateClient();
                var response = await client.GetAsync(path);
                var result = await response.Content.ReadAsStringAsync();
                Assert.True(response.IsSuccessStatusCode);
                Assert.Equal(relativePath, TestHelpers.GetEnvVarFromOuptut(result, "PATH_INFO"));
            }

        }

        [Fact]
        public async Task Test_Path_Info_Is_Empty()
        {
            (string path, string relativePath)[] tests =
            {
                ("/","")
            };

            var mockLogger = new Mock<ILogger>();
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
            var testServer = TestHelpers.CreateTestServer(new string[] { "testdata/testPATH_INFO_EMPTYSettings.json" }, mockLoggerFactory);

            foreach (var (path, relativePath) in tests)
            {
                var client = testServer.CreateClient();
                var response = await client.GetAsync(path);
                var result = await response.Content.ReadAsStringAsync();
                Assert.True(response.IsSuccessStatusCode);
                Assert.Equal(relativePath, TestHelpers.GetEnvVarFromOuptut(result, "PATH_INFO"));
            }

        }
    }
}
