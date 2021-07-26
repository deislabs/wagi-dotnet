using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BindleSource.Test
{
    public class BindleSourceTest
    {
        private readonly WebApplicationFactory<Startup> factory;
        public BindleSourceTest()
        {
            factory = new WebApplicationFactory<Startup>();
        }
        [Fact]
        public async Task TestInvokeVersion110()
        {
            var baseAddress = new Uri("http://127.0.0.1:5004");
            var client = factory.CreateDefaultClient(baseAddress);

            var response = await client.GetAsync("/1.1.0");
            var result = await response.Content.ReadAsStringAsync();
            Assert.Equal("Kia ora, world from 1.1.0!", result.TrimEnd());
            Assert.True(response.IsSuccessStatusCode);

            baseAddress = new Uri("http://127.0.0.1:5005");
            client = factory.CreateDefaultClient(baseAddress);

            response = await client.GetAsync("/1.1.0");
            result = await response.Content.ReadAsStringAsync();
            Assert.Equal("Kia ora, world from 1.1.0!", result.TrimEnd());
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task TestInvokeVersion1()
        {
            var baseAddress = new Uri("http://127.0.0.1:5003");
            var client = factory.CreateDefaultClient(baseAddress);

            var response = await client.GetAsync("/v1");
            var result = await response.Content.ReadAsStringAsync();
            Assert.Equal("Kia ora, world from 1.1.0!", result.TrimEnd());
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task TestInvokeRoot()
        {
            var client = factory.CreateClient();

            var response = await client.GetAsync("/");
            var result = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello, world from 1.0.0!", result.TrimEnd());
            Assert.True(response.IsSuccessStatusCode);
        }
    }
}
