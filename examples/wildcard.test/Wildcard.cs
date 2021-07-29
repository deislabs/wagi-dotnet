using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Wildcard.Test
{
    public class WildcardTest
    {
        private readonly WebApplicationFactory<Startup> factory;
        public WildcardTest()
        {
            factory = new WebApplicationFactory<Startup>();
        }
        [Fact]
        public async Task TestWildcardRoute()
        {
            (string path, string route)[] tests =
            {
                ("/path","/path"),
                ("/path/","/path"),
                ("/path/other","/path/..."),
                ("/path/other/","/path/..."),
                ("/path/some/other","/path/..."),
                ("/some/other/path","/..."),
                ("/someotherroute","/..."),
                ("/someotherroute/","/...")
            };

            foreach (var (path, route) in tests)
            {
                var client = factory.CreateClient();
                var response = await client.GetAsync(path);
                var result = await response.Content.ReadAsStringAsync();
                Assert.True(response.IsSuccessStatusCode);
                Assert.Equal(route, GetRouteValue(result));
            }

        }
        private static string GetRouteValue(string result)
        {
            string line;
            using (var reader = new StringReader(result))
            {
                while ((line = reader.ReadLine()) is not null)
                {
                    if (line.StartsWith("X_MATCHED_ROUTE"))
                    {
                        return line.Split('=')[1]?.Trim() ?? null;
                    }
                }
            }
            return null;
        }
    }
}
