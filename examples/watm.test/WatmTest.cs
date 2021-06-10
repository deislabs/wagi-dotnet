using Xunit;
using Watm;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

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
    public async Task TestInvokeWASM()
    {
      var client = factory.CreateClient();

      var response = await client.GetAsync("/hellowat");
      var result = await response.Content.ReadAsStringAsync();
      Assert.Equal("Hello World!", result.TrimEnd());
      Assert.True(response.IsSuccessStatusCode);

    }
  }
}
