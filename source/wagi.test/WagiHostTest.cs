
using System;
using System.IO;
using System.Threading.Tasks;
using Deislabs.Wagi.Test;
using Deislabs.Wagi.Test.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Deislabs.Wagi.Test
{
    public class WagiHostTest
    {
        [Fact]
        public async Task Test_X_Relative_Path()
        {
            (string path, string relativePath)[] tests =
            {
                ("/path",""),
                ("/path/other","other"),
                ("/path/some/other","some/other"),
                ("/some/other/path","some/other/path"),
                ("/someotherroute","someotherroute")
            };

            var mockLogger = new Mock<ILogger>();
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
            var testServer = TestHelpers.CreateTestServer("testdata/testX_RELATIVE_PATHSettings.json", mockLoggerFactory);

            foreach (var test in tests)
            {
                var client = testServer.CreateClient();
                var response = await client.GetAsync(test.path);
                var result = await response.Content.ReadAsStringAsync();
                Assert.True(response.IsSuccessStatusCode);
                Assert.Equal(test.relativePath, TestHelpers.GetEnvVarFromOuptut(result, "X_RELATIVE_PATH"));
            }
        }
    }
}
