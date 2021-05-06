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

namespace Deislabs.WAGI.Test
{
  public class StartupTest
  {
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddRouting();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      app.UseRouting();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapWASMModules();
      });

    }
  }
}
