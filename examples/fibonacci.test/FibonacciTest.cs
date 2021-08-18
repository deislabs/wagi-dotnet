using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Fibonacci.Test
{
    public class FibonacciTest
    {
        private readonly WebApplicationFactory<Startup> factory;
        public FibonacciTest()
        {
            factory = new WebApplicationFactory<Startup>();
        }
        [Fact]
        public async Task TestInvokeWagi()
        {
            var client = factory.CreateClient();

            var response = await client.GetAsync("/fibonacci?23");
            var result = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal("fib(23)=28657", result.TrimEnd());
        }
    }
}
