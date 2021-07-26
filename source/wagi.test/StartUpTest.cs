using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Deislabs.Wagi.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Xunit.Sdk;

namespace Deislabs.Wagi.Test
{
    public class StartupTest
    {
        private readonly string sectionName;
        private readonly bool expectException = false;
        private readonly Type exceptionType;
        private readonly string exceptionMessage;
        public IConfiguration Configuration { get; }

        public StartupTest(IConfiguration configuration, string sectionName = null)
        {
            this.sectionName = sectionName;
            this.Configuration = configuration;
        }
        public StartupTest(IConfiguration configuration, Type exceptionType, string exceptionMessage, string sectionName = null) : this(configuration, sectionName)
        {
            this.expectException = true;
            this.exceptionType = exceptionType;
            this.exceptionMessage = exceptionMessage;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();
            if (string.IsNullOrEmpty(sectionName))
            {
                services.AddWagi(Configuration);
            }
            else
            {
                services.AddWagi(Configuration, sectionName);
            }

            services.AddRouting();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                var exception = Record.Exception(() => endpoints.MapWagiModules());
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
