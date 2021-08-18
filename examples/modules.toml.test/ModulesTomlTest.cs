using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Modules.Toml.Test
{
    public class ModulesTomlTest
    {
        private readonly WebApplicationFactory<Startup> factory;
        public ModulesTomlTest()
        {
            factory = new WebApplicationFactory<Startup>();
        }
        [Fact]
        public async Task TestInvokeWagi()
        {
            var client = factory.CreateClient();

            var response = await client.GetAsync("/static/README.md");
            var result = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal("# Modules.toml example", result.TrimEnd());
        }
    }
}
