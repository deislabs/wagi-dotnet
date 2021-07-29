using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Routes.Test
{
    public class RoutesTest
    {
        private readonly WebApplicationFactory<Startup> factory;
        public RoutesTest()
        {
            factory = new WebApplicationFactory<Startup>();
        }
        [Fact]
        public async Task TestModuleDefinedRoutes()
        {
            (string path, string expectedResponse)[] tests =
            {
                ("/example","Hello from main()"),
                ("/example/main","Hello from main()"),
                ("/example/main/","Hello from main()"),
                ("/example/hello","Hello"),
                ("/example/hello/","Hello"),
                ("/example/goodbye","Goodbye"),
                ("/example/goodbye/","Goodbye"),
                ("/example/goodbye/world","Goodbye")
            };

            foreach (var (path, expectedResponse) in tests)
            {
                var client = factory.CreateClient();
                var response = await client.GetAsync(path);
                var result = await response.Content.ReadAsStringAsync();
                Assert.True(response.IsSuccessStatusCode);
                Assert.Equal(expectedResponse, result.Trim());
            }

        }
    }
}
