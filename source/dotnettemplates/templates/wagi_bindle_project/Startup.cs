using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Deislabs.Wagi.Extensions;

namespace Wagi.Project
{
    public class Startup
    {

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {

            // HTTP Client is required for wasi_experimentatl_http support.
            services.AddHttpClient();
            // This adds the services required for using WAGI Modules, by default configuration is expected to be found in a section named Wagi, if the section is renamed then the name should be passed as the second argument.
            services.AddWagi(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                // This builds the routes defined by Wagi Modules, if configuration changes the endpoint routing will be automatically updated.
                endpoints.MapWagiModules();
            });
        }
    }
}
