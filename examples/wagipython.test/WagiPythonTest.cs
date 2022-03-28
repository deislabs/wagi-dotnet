using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace WagiPython.Test
{
    public class WagiPythonTest
    {
        private readonly WebApplicationFactory<Startup> factory;
        public WagiPythonTest()
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
            var line = reader.ReadLine();
            Assert.Equal("Hello from python on wasi", line);
            _ = reader.ReadLine();
            line = reader.ReadLine();
            Assert.Equal("### Arguments ###", line);
             _ = reader.ReadLine();
            line = reader.ReadLine();
            Assert.Equal("['/code/env.py', '1', '2', '3']", line);
        }
    }
}
