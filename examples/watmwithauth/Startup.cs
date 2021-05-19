using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Deislabs.WAGI.Extensions;
using System.Security.Claims;

namespace watmwithauth
{
  public class Startup
  {
    public Startup()
    {
    }

    public void ConfigureServices(IServiceCollection services)
    {
      services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
          options.Cookie.HttpOnly = true;
          options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
          options.LoginPath = "/Home/Login";
          options.AccessDeniedPath = "/Home/AccessDenied";
          options.Cookie.Name = "WAGIDemo";

        });
      services.AddAuthorization(options =>
        {
          options.AddPolicy("IsSpecial", policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c =>
                    c.Type == ClaimTypes.UserData &&
                    c.Value == "IsSpecialAdmin")));
        });

      services.AddControllersWithViews();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      app.UseRouting();
      app.UseStaticFiles();
      app.UseAuthentication();
      app.UseAuthorization();


      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllerRoute(
                  name: "default",
                  pattern: "{controller=Home}/{action=Login}");
        endpoints.MapWASMModules();
      });
    }
  }
}
