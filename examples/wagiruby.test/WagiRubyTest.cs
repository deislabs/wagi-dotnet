using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace WagiRuby.Test
{
    public class WagiRubyTest
    {
        private readonly WebApplicationFactory<Startup> factory;
        public WagiRubyTest()
        {
            factory = new WebApplicationFactory<Startup>();
        }
        [Fact]
        public async Task TestInvokeWagi()
        {
            var client = factory.CreateClient();

            var response = await client.GetAsync("/?1&2&3");
            var result = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode);
            var reader = new StringReader(result);
            _ = reader.ReadLine();
            var line = reader.ReadLine();
            Assert.Equal("Hello from ruby!", line);
            _ = reader.ReadLine();
            line = reader.ReadLine();
            Assert.Equal("ruby version: 3.2.0 (2022-02-10) [wasm32-wasi]", line);
            _ = reader.ReadLine();
            line = reader.ReadLine();
            Assert.Equal("### Arguments ###", line);
             _ = reader.ReadLine();
            line = reader.ReadLine();
            Assert.Equal("arg 0: 1", line);
            line = reader.ReadLine();
            Assert.Equal("arg 1: 2", line);
            line = reader.ReadLine();
            Assert.Equal("arg 2: 3", line);
        }
    }
}
