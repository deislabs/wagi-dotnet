using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Deislabs.WAGI.Extensions;
using Xunit;
using Xunit.Sdk;

namespace Deislabs.WAGI.Test
{
  public class StartupTest
  {
    private readonly string sectionName;
    private readonly bool expectException = false;
    private readonly Type exceptionType;
    private readonly string exceptionMessage;

    public StartupTest()
    {
      this.sectionName = "WASM";
    }
    public StartupTest(string sectionName)
    {
      this.sectionName = sectionName ?? "WASM";
    }
    public StartupTest(Type exceptionType, string exceptionMessage, string sectionName = "WASM")
    {
      this.expectException = true;
      this.exceptionType = exceptionType;
      this.exceptionMessage = exceptionMessage;
      this.sectionName = sectionName;
    }
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddRouting();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      app.UseRouting();

      app.UseEndpoints(endpoints =>
      {
        var exception = Record.Exception(() => endpoints.MapWASMModules(this.sectionName));
        if (expectException)
        {
          Assert.NotNull(exception);
          Assert.IsType(this.exceptionType, exception);
          Assert.Equal(this.exceptionMessage, exception.Message);
        }
        else
        {
          Assert.Null(exception);
        }
      });

    }
  }
}
