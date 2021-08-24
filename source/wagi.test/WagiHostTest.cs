using System.Net.Http;
using System.Threading.Tasks;
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
        [Fact]
        public async Task Test_Headers()
        {

            (string header, string value)[] tests = {

                ("X_MATCHED_ROUTE", "/path/..."),
                ("HTTP_ACCEPT", "text/html"),
                ("REQUEST_METHOD", "POST"),
                ("SERVER_PROTOCOL", "HTTP/1.1"),
                ("HTTP_USER_AGENT", "test-agent"),
                ("HTTP_CONTENT_TYPE", "text/plain; charset=utf-8"),
                ("HTTP_CONTENT_LENGTH", "4"),
                ("SCRIPT_NAME", "/path"),
                ("SERVER_SOFTWARE", "WAGI/1"),
                ("SERVER_PORT", "80"),
                ("SERVER_NAME", "example.com"),
                ("AUTH_TYPE", ""),
                ("PATH_INFO", "/test;run"),
                ("PATH_TRANSLATED", "/test;run"),
                ("X_RAW_PATH_INFO", "%2Ftest%3Brun"),
                ("QUERY_STRING", "foo=bar&a=b"),
                ("CONTENT_LENGTH", "4"),
                ("HTTP_HOST", "example.com:80"),
                ("GATEWAY_INTERFACE", "CGI/1.1"),
                ("REMOTE_USER", ""),
                ("X_FULL_URL", "http://example.com:80/path%2Ftest%3Brun?foo=bar&a=b"),

                // Extra header should be passed through
                ("HTTP_X_TEST_HEADER", "hello")

            };

            var mockLogger = new Mock<ILogger>();
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
            var testServer = TestHelpers.CreateTestServer(new string[] { "testdata/testHeadersSettings.json" }, mockLoggerFactory);
            var client = testServer.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Header", "hello");
            client.DefaultRequestHeaders.Add("User-Agent", "test-agent");
            client.DefaultRequestHeaders.Add("Accept", "text/html");
            client.DefaultRequestHeaders.Add("Host", "example.com:80");
            client.DefaultRequestHeaders.Add("Authorization", "supersecret");
            client.DefaultRequestHeaders.Add("Connection", "sensitive");

            var response = await client.PostAsync("/path/test%3brun?foo=bar&a=b", new StringContent("Test"));
            var result = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode);

            foreach (var (header, value) in tests)
            {
                Assert.Equal(value, TestHelpers.GetEnvVarFromOuptut(result, header));
            }


            // Security-sensitive headers should be removed.
            Assert.Null(TestHelpers.GetEnvVarFromOuptut(result, "HTTP_AUTHORIZATION"));
            Assert.Null(TestHelpers.GetEnvVarFromOuptut(result, "HTTP_CONNECTION"));

        }
    }
}
