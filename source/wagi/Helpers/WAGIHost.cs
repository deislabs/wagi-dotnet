namespace Deislabs.WAGI.Helpers
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Globalization;
  using System.IO;
  using System.Linq;
  using System.Runtime.CompilerServices;
  using System.Text;
  using System.Threading.Tasks;
  using Deislabs.WAGI.Extensions;
  using Microsoft.AspNetCore.Http;
  using Microsoft.AspNetCore.Http.Features;
  using Microsoft.AspNetCore.Routing;
  using Microsoft.CSharp.RuntimeBinder;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.Logging;
  using Microsoft.Extensions.Primitives;
  using Wasmtime;
  using Wasmtime.Exports;
  using static System.Net.WebUtility;

  /// <summary>
  /// WAGIHost runs WAGI Modules.
  /// </summary>
  internal class WAGIHost
  {
    private const string Version = "CGI/1.1";
    private const string ServerVersion = "WAGI/1";
    private readonly HttpContext context;
    private readonly ILogger logger;
    private readonly string entryPoint;
    private readonly IDictionary<string, string> volumes;
    private readonly string wasmFile;
    private readonly string moduleType;

    /// <summary>
    /// Initializes a new instance of the <see cref="WAGIHost"/> class.
    /// </summary>
    /// <param name="context">HttpContext for the request.</param>
    /// <param name="entryPoint">entryPoint to call in the WASM Module. </param>
    /// <param name="wasmFile">The WASM File name.</param>
    /// <param name="moduleType">Type of the module, can be either WASM or WAT.</param>
    /// <param name="volumes">The volumes to be added to the WasiConfiguration as preopened directories.</param>
    public WAGIHost(HttpContext context, string entryPoint, string wasmFile, string moduleType, IDictionary<string, string> volumes)
    {
      this.context = context;
      var loggerFactory = context.RequestServices.GetService<ILoggerFactory>();
      this.logger = loggerFactory.CreateLogger(typeof(WAGIHost).Namespace);
      this.entryPoint = entryPoint ?? "_start";
      this.volumes = volumes;
      this.wasmFile = wasmFile;
      this.moduleType = moduleType;
    }

    /// <summary>
    /// Processes a WAGI Request.
    /// </summary>
    public async Task ProcessRequest()
    {
      using var stdin = new TempFile();
      using var stdout = new TempFile();
      using var stderr = new TempFile();
      var input = await this.GetInput();
      using StreamWriter writer = new(stdin.Path);
      await writer.WriteAsync(input);
      var envvars = this.CreateWAGIEnvVars();
      var args = this.GetArgs();
      var config = new WasiConfiguration()
        .WithStandardOutput(stdout.Path)
        .WithStandardInput(stdin.Path)
        .WithStandardError(stderr.Path)
        .WithArgs(args)
        .WithEnvironmentVariables(envvars)
        .WithVolumes(this.volumes, this.logger);

      using dynamic instance = this.GetWasmtimeInstance(config);
      var callSiteBinder = Binder.InvokeMember(CSharpBinderFlags.None, this.entryPoint, Enumerable.Empty<Type>(), instance.GetType(), new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
      var callSite = CallSite<Action<CallSite, object>>.Create(callSiteBinder);

      var stopWatch = Stopwatch.StartNew();
      callSite.Target(callSite, instance);
      stopWatch.Stop();
      var elapsed = stopWatch.Elapsed;
      this.logger.LogTrace($"Call Module {this.wasmFile} Function {this.entryPoint} Args {string.Join(",", args)} Complete in {elapsed.TotalSeconds:00}:{elapsed.Milliseconds:000} seconds");

      using var errreader = new StreamReader(stderr.Path);
      string line;
      while ((line = await errreader.ReadLineAsync()) != null)
      {
        this.logger.LogError($"Error from Module {this.wasmFile} Function {this.entryPoint} Args {string.Join(",", args)} . Error:{line}");
      }

      await this.ProcessOutput(stdout.Path);
    }

    private List<(string Key, string Value)> CreateWAGIEnvVars()
    {
      var environmentVariables = new List<(string, string)>();
      var headers = this.context.Request.Headers;
      var req = this.context.Request;
      var routeData = this.context.GetRouteData();

      // TODO: implement AUTH_TYPE
      environmentVariables.Add(("AUTH_TYPE", string.Empty));

      if (req.ContentLength > 0)
      {
        environmentVariables.Add(("CONTENT_LENGTH", Convert.ToString(req.ContentLength, CultureInfo.InvariantCulture)));
      }

      environmentVariables.Add(("CONTENT_TYPE", req.ContentType));
      environmentVariables.Add(("X_FULL_URL", $"{req.Scheme}://{req.Host}{req.Path}{req.QueryString}"));
      environmentVariables.Add(("GATEWAY_INTERFACE", Version));
      environmentVariables.Add(("X_MATCHED_ROUTE", routeData.Values["key"]?.ToString() ?? string.Empty));
      environmentVariables.Add(("PATH_INFO", req.Path));

      // TODO: implement Path Translated
      environmentVariables.Add(("PATH_TRANSLATED", req.Path));
      environmentVariables.Add(("QUERY_STRING", req.QueryString.Value));
      environmentVariables.Add(("REMOTE_ADDR", this.context.Connection.RemoteIpAddress.ToString()));
      environmentVariables.Add(("REMOTE_HOST", this.context.Connection.RemoteIpAddress.ToString()));

      // TODO: set Remote User
      environmentVariables.Add(("REMOTE_USER", string.Empty));
      environmentVariables.Add(("REQUEST_METHOD", req.Method));
      environmentVariables.Add(("SCRIPT_NAME", routeData.Values["name"]?.ToString() ?? string.Empty));
      environmentVariables.Add(("SERVER_NAME", req.Host.Host));
      environmentVariables.Add(("SERVER_PORT", Convert.ToString(req.Host.Port ?? 80, CultureInfo.InvariantCulture)));
      environmentVariables.Add(("SERVER_PROTOCOL", req.Scheme));
      environmentVariables.Add(("SERVER_SOFTWARE", ServerVersion));

      foreach (var header in headers)
      {
        var key = $"HTTP_{header.Key.Replace("-", "_", StringComparison.InvariantCultureIgnoreCase)}";
        if (key == "HTTP_AUHTORIZATION" || key == "HTTP_CONNECTION")
        {
          continue;
        }

        environmentVariables.Add((key, header.Value.ToString()));
      }

      return environmentVariables;
    }

    private string[] GetArgs()
    {
      var args = this.context.Request.QueryString.Value.Length > 0 ? this.context.Request.QueryString.Value.TrimStart('?').Split("&") : Array.Empty<string>();
      return args
        .Select(a => UrlDecode(a))
        .ToArray();
    }

    private async Task<string> GetInput()
    {
      var input = this.context.Request.Body;
      using StreamReader reader = new(input);

      if (input.CanSeek)
      {
        input.Seek(0, SeekOrigin.Begin);
      }

      return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    private async Task ProcessOutput(string outputPath)
    {
      var headers = new List<string>();
      var responseBuilder = new StringBuilder();
      var sufficientResponse = false;
      var endofHeaders = false;
      var statusCode = 200;
      var contentType = string.Empty;
      var reason = string.Empty;
      using var reader = new StreamReader(outputPath);
      string line;
      while ((line = await reader.ReadLineAsync()) != null)
      {
        if (endofHeaders)
        {
          responseBuilder.AppendLine(line);
        }
        else
        {
          if (line.Length == 0)
          {
            endofHeaders = true;
          }
          else
          {
            headers.Add(line);
          }
        }
      }

      headers.ForEach(header =>
      {
        switch (header)
        {
          case var _ when header.StartsWith("location:", StringComparison.InvariantCultureIgnoreCase):
            this.AddHeader("Location", new StringValues(header.Split(':')[1]?.TrimStart().Split(',')));
            sufficientResponse = true;
            break;
          case var _ when header.StartsWith("content-type:", StringComparison.InvariantCultureIgnoreCase):
            var val = header.Split(':')[1]?.TrimStart();
            this.AddHeader("Content-Type", new StringValues(val));
            contentType = val;
            sufficientResponse = true;
            break;
          case var _ when header.StartsWith("status:", StringComparison.InvariantCultureIgnoreCase):
            var headerValue = header.Split(':')[1].TrimStart();
            if (headerValue.Contains(' ', StringComparison.InvariantCulture))
            {
              statusCode = Convert.ToInt32(headerValue.Split(" ")[0], CultureInfo.InvariantCulture);
              reason = headerValue.Split(" ")[1];
            }
            else
            {
              statusCode = Convert.ToInt32(headerValue, CultureInfo.InvariantCulture);
            }

            sufficientResponse = true;
            break;
          default:
            this.AddHeader(header.Split(':')[0], new StringValues(header.Split(':')[1]?.TrimStart().Split(',')));
            break;
        }
      });

      if (!sufficientResponse)
      {
        throw new ApplicationException("Module did not produce either location or content-type headers");
      }

      if (contentType.Length > 0)
      {
        this.context.Response.ContentType = contentType;
      }

      this.context.Response.StatusCode = statusCode;
      if (reason.Length > 0)
      {
        this.context.Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = reason;
      }

      if (responseBuilder.Length > 0)
      {
        await this.context.Response.WriteAsync(responseBuilder.ToString());
      }
    }

    private Instance GetWasmtimeInstance(WasiConfiguration config)
    {
      using var engine = new Engine();
      using var module = this.GetWasmtimeModule(engine);
      _ = module.Exports.Functions.SingleOrDefault<FunctionExport>(f => f.Name == this.entryPoint) ?? throw new ArgumentException("function", $"function {this.entryPoint} is not exported by {this.wasmFile}");
      using var host = new Host(engine);
      host.DefineWasi("wasi_snapshot_preview1", config);
      return host.Instantiate(module);
    }

    private Module GetWasmtimeModule(Engine engine)
    {
      return this.moduleType switch
      {
        "WASM" => Module.FromFile(engine, this.wasmFile),
        "WAT" => Module.FromTextFile(engine, this.wasmFile),
        _ => throw new ArgumentException($"invalid module type {this.moduleType} for File {this.wasmFile}"),
      };
    }

    private void AddHeader(string key, string value) => this.context.Response.Headers.TryAdd(key, value);
  }
}
