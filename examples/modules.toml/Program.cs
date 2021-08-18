
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Deislabs.Wagi.Configuration.Modules.Toml;

namespace Modules.Toml
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                }).ConfigureAppConfiguration(builder =>
                {
                    builder.AddModulesTomlFile("modules.toml", false, true);
                });
    }
}
