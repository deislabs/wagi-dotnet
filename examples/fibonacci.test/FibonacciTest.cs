using System;
using System.Net.Http;
using Xunit;
using Fibonacci;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

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
    public async Task TestInvokeWASM()
    {
      var client = factory.CreateClient();

      var response = await client.GetAsync("/fibonacci?23");
      var result = await response.Content.ReadAsStringAsync();
      Assert.Equal("fib(23)=28657\n", result);
      Assert.True(response.IsSuccessStatusCode);

    }
  }
}
