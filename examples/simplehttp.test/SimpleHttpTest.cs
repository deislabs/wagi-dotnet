using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;
using Simplehttp;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace SimpleHttp.Test
{
  public class SimplehttpTestFixture
  {
    public static string TestStorageAccountNameEnvVar = "TEST_STORAGE_ACCOUNT_NAME";
    public static string TestStorageAccountKeyEnvVar = "TEST_STORAGE_ACCOUNT_KEY";
    public string BlobName {get;private set;}
    public string AccountName {get; private set;}
    public string AccountKey {get; private set;}
    public string ContainerName {get; private set;}
    public string TestPostData {get; private set;}
    public SimplehttpTestFixture()
    {
      this.BlobName = Guid.NewGuid().ToString();
      this.AccountName = Environment.GetEnvironmentVariable(SimplehttpTestFixture.TestStorageAccountNameEnvVar);
      this.AccountKey = Environment.GetEnvironmentVariable(SimplehttpTestFixture.TestStorageAccountKeyEnvVar);
      // TODO: create the container if not existing
      this.ContainerName="wagitest";
      this.TestPostData="Hello from wagi-experimental-http";

    }
  }

  public sealed class SkipIfStorageEnvVarsNotSetFact : FactAttribute
  {
    public SkipIfStorageEnvVarsNotSetFact() {
        if (Environment.GetEnvironmentVariable(SimplehttpTestFixture.TestStorageAccountNameEnvVar).Length == 0 ||Environment.GetEnvironmentVariable(SimplehttpTestFixture.TestStorageAccountKeyEnvVar).Length == 0) 
        {
          this.Skip = "Storage Test Env Vars are not set";
        }
    }
  }
  public class SimplehttpTest: IClassFixture<SimplehttpTestFixture>
  {
    private readonly SimplehttpTestFixture fixture;
    private readonly WebApplicationFactory<Startup> factory;
    public SimplehttpTest(SimplehttpTestFixture fixture)
    {
      this.factory = new WebApplicationFactory<Startup>();
      this.fixture = fixture;
    }
    [Fact]
    public async Task TestPostManEcho()
    {
      var client = factory.CreateClient();
      var content = new StringContent( this.fixture.TestPostData);
      var response = await client.PostAsync("/test", content);
      var result = await response.Content.ReadFromJsonAsync<JsonElement>();
      Assert.True(response.IsSuccessStatusCode);
      Assert.Equal( this.fixture.TestPostData, result.GetProperty("data").GetString());
    }

    [SkipIfStorageEnvVarsNotSetFact]
    public async Task TestReadWriteBlob()
    {
      var configData=$@"
      {{
        ""WASM"": {{
          ""Modules"": {{
            ""/writeblob"": {{
              ""Environment"" :{{
                ""STORAGE_ACCOUNT"":""{this.fixture.AccountName}"",
                ""STORAGE_MASTER_KEY"" : ""{this.fixture.AccountKey}""
              }},
              ""AllowedHosts"": [
                ""https://{this.fixture.AccountName}.blob.core.windows.net""
              ]
            }},
            ""/readblob"": {{
              ""Environment"" :{{
                ""STORAGE_ACCOUNT"":""{this.fixture.AccountName}"",
                ""STORAGE_MASTER_KEY"" : ""{this.fixture.AccountKey}""
              }},
              ""AllowedHosts"": [
                ""https://{this.fixture.AccountName}.blob.core.windows.net""
              ]
            }}
          }}
        }}
      }}";
      var client = factory.WithWebHostBuilder(
        builder => 
        {
          builder.ConfigureAppConfiguration((_, config) =>
          {
            config.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(configData)));
          });
        }
       ).CreateClient();
      var content = new StringContent(this.fixture.TestPostData);
      var response = await client.PostAsync($"/writeblob?container={this.fixture.ContainerName}&blob={this.fixture.BlobName}", content);
      var result = await response.Content.ReadAsStringAsync();
      Assert.True(response.IsSuccessStatusCode);
      Assert.Equal($"Writing {this.fixture.TestPostData.Length} bytes.\n", result);
      response = await client.GetAsync($"/readblob?container={this.fixture.ContainerName}&blob={this.fixture.BlobName}");
      result = await response.Content.ReadAsStringAsync();
      Assert.True(response.IsSuccessStatusCode);
      Assert.Equal($"{this.fixture.TestPostData}\n", result);
    }
  }
}
