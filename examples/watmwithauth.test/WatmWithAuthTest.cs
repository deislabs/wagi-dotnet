using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using watmwithauth;
using System.Net;
using System.Web;
using System.Net.Http;
using System.Text;

namespace watmwithauth.test
{
  public class WatmWithAuthTest
  {
    private readonly WebApplicationFactory<Startup> factory;
    public WatmWithAuthTest()
    {
      factory = new WebApplicationFactory<Startup>();
    }
    [Fact]
    public async Task TestRequiresAuth()
    {
      factory.ClientOptions.AllowAutoRedirect = false;
      var client = factory.CreateClient();

      var routes = new string[] { "/hellowatauth", "/hellowatrole", "/hellowatpolicy" };

      foreach (var route in routes)
      {
        var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"http://localhost/Home/Login?ReturnUrl={HttpUtility.UrlEncode(route)}", response.Headers.Location.ToString(), true);
      }

    }

    [Fact]
    public async Task TestAuthSucceeds()
    {
      factory.ClientOptions.AllowAutoRedirect = true;
      var client = factory.CreateClient();

      var tests = new (string path, string user)[] { ("/hellowatauth", "admin"), ("/hellowatrole", "superadmin"), ("/hellowatpolicy", "specialadmin") };
      foreach (var test in tests)
      {
        var content = new StringContent($"username={test.user}&password=admin", Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await client.PostAsync($"http://localhost/Home/Login?ReturnUrl={test.path}", content);
        var result = await response.Content.ReadAsStringAsync();
        Assert.Equal("Hello World!", result.TrimEnd());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
      }
    }

    [Fact]
    public async Task TestAccessDenied()
    {
      factory.ClientOptions.AllowAutoRedirect = true;
      var client = factory.CreateClient();

      var tests = new (string path, string user)[] { ("/hellowatrole", "admin"), ("/hellowatpolicy", "admin") };
      foreach (var test in tests)
      {
        var content = new StringContent($"username={test.user}&password=admin", Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await client.PostAsync($"http://localhost/Home/Login?ReturnUrl={test.path}", content);
        var result = await response.Content.ReadAsStringAsync();
        Assert.Contains($"Access Denied. User <b>{test.user}</b> does not have access to <b> {test.path}</b>", result);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
      }
    }
  }
}
