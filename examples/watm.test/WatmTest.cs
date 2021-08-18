using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Watm;
using Xunit;

namespace Watm.Test
{
    public class WatmTest
    {
        private readonly WebApplicationFactory<Startup> factory;
        public WatmTest()
        {
            factory = new WebApplicationFactory<Startup>();
        }
        [Fact]
        public async Task TestInvokeWagi()
        {
            var client = factory.CreateClient();

            var response = await client.GetAsync("/hellowat");
            var result = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal("Hello World!", result.TrimEnd());
        }
    }
}
