[assembly: System.CLSCompliant(false)]

namespace Deislabs.Wagi.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.IO.Pipelines;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Authentication;
    using System.Text;
    using System.Threading.Tasks;
    using Deislabs.Wagi.DataSource;
    using Deislabs.Wagi.Extensions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;
    using Wasi.Experimental.Http;
    using Wasmtime;

    using static System.Net.WebUtility;

    /// <summary>
    /// WagiHost runs Wagi Modules.
    /// </summary>
    internal class WagiHost
    {
        private const string Version = "CGI/1.1";
        private const string ServerVersion = "WAGI/1";
        private readonly HttpContext context;
        private readonly ILogger logger;
        private readonly ILoggerFactory loggerFactory;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly string entryPoint;
        private readonly IDictionary<string, string> volumes;
        private readonly IDictionary<string, string> environment;
        private readonly string wasmFile;
        private readonly List<Uri> allowedHosts;
        private readonly IModuleResolver moduleResolver;
        private readonly int maxHttpRequests;
        private string pathInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="WagiHost"/> class.
        /// </summary>
        /// <param name="context">HttpContext for the request.</param>
        /// <param name="httpClientFactory">IHttpClientFactory to be used for module Http Requests. </param>
        /// <param name="entryPoint">entryPoint to call in the WASM Module. </param>
        /// <param name="wasmFile">The WASM File name.</param>
        /// <param name="moduleResolver">Module resolver to get wasmtime Module and Engine.</param>
        /// <param name="volumes">The volumes to be added to the WasiConfiguration as preopened directories.</param>
        /// <param name="environment">The environment variables to be added to the WasiConfiguration.</param>
        /// <param name="allowedHosts">A set of allowedHosts (hostnames) that the module can send HTTP requests to.</param>
        /// <param name="maxHttpRequests">The maximum number of HTTP Requests that the module can make.</param>
        public WagiHost(HttpContext context, IHttpClientFactory httpClientFactory, string entryPoint, string wasmFile, IModuleResolver moduleResolver, IDictionary<string, string> volumes, IDictionary<string, string> environment, List<Uri> allowedHosts, int maxHttpRequests)
        {
            this.context = context;
            this.httpClientFactory = httpClientFactory;
            var loggerFactory = context.RequestServices.GetService<ILoggerFactory>();
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory.CreateLogger(typeof(WagiHost).FullName);
            this.entryPoint = entryPoint ?? "_start";
            this.volumes = volumes;
            this.wasmFile = wasmFile;
            this.environment = environment ?? new Dictionary<string, string>();
            this.allowedHosts = allowedHosts;
            this.maxHttpRequests = maxHttpRequests;
            this.moduleResolver = moduleResolver;
        }

        /// <summary>
        /// Processes a Wagi Request.
        /// </summary>
        public async Task ProcessRequest()
        {
            using var stdin = new TempFile();
            using var stdout = new TempFile();
            using var stderr = new TempFile();
            await this.WriteRequestBody(stdin);
            var config = this.GetWasiConfiguration(stdin, stdout, stderr);
            var engine = this.moduleResolver.Engine;
            using var linker = new Linker(engine);
            using var store = new Store(engine);
            store.SetWasiConfiguration(config);
            linker.DefineWasi();

            var module = this.moduleResolver.GetWasmModule(this.wasmFile);
            _ = module.Exports.ToList<Export>().SingleOrDefault(f => (f.Name == this.entryPoint && f is FunctionExport)) ?? throw new ArgumentException("function", $"function {this.entryPoint} is not exported by {this.wasmFile}");
            using var httpRequestHandler = this.GetHttpRequestHandler(linker, store);
            {
                try
                {
                    var stopWatch = Stopwatch.StartNew();
                    var instance = linker.Instantiate(store, module);
                    var entrypoint = instance.GetFunction(store, this.entryPoint);
                    entrypoint.Invoke(store);
                    stopWatch.Stop();
                    var elapsed = stopWatch.Elapsed;
                    this.logger.ModuleExecutionTime(this.wasmFile, this.entryPoint, elapsed.TotalSeconds, elapsed.Milliseconds, elapsed.Ticks);
                }
                catch (WasmtimeException ex)
                {
                    if (ex.Message == "unknown import: `wasi_experimental_http::close` has not been defined")
                    {
                        throw new ApplicationException("Allowed Hosts must be configured for modules making HTTP requests", ex);
                    }

                    throw;
                }
            }

            using var stderrStream = new FileStream(stderr.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var errreader = new StreamReader(stderrStream);

            string line;
            while ((line = await errreader.ReadLineAsync()) != null)
            {
                this.logger.ModuleWroteToStdErr(this.wasmFile, this.entryPoint, line.TrimEnd('\0'));
            }

            await this.ProcessOutput(stdout.Path);
        }

        private WasiConfiguration GetWasiConfiguration(TempFile stdin, TempFile stdout, TempFile stderr)
        {
            var environmentVariables = this.CreateWAGIEnvVars();
            var args = this.GetArgs();
            return new WasiConfiguration()
              .WithStandardOutput(stdout.Path)
              .WithStandardInput(stdin.Path)
              .WithStandardError(stderr.Path)
              .WithArgs(args)
              .WithEnvironmentVariables(environmentVariables)
              .WithEnvironment(this.environment, this.logger)
              .WithVolumes(this.volumes, this.logger);
        }

        private HttpRequestHandler GetHttpRequestHandler(Linker linker, Store store)
        {
            HttpRequestHandler httpRequestHandler = null;
            if (this.allowedHosts.Count > 0)
            {
                httpRequestHandler = new HttpRequestHandler(linker, store, this.loggerFactory, this.httpClientFactory, this.maxHttpRequests, this.allowedHosts);
            }

            return httpRequestHandler;
        }

        private async Task WriteRequestBody(TempFile stdin)
        {
            var input = await this.GetInput();
            using StreamWriter writer = new(stdin.Path);
            await writer.WriteAsync(input);
            await writer.FlushAsync();
            writer.Close();
        }

        private List<(string Key, string Value)> CreateWAGIEnvVars()
        {
            var environmentVariables = new List<(string, string)>();
            var headers = this.context.Request.Headers;
            var req = this.context.Request;
            var endpoint = this.context.GetEndpoint();
            var originalRoute = this.context.GetEndpoint().Metadata.Where(a => a.GetType() == typeof(WagiRouteAttribute)).FirstOrDefault() as WagiRouteAttribute;

            // TODO: implement AUTH_TYPE
            environmentVariables.Add(("AUTH_TYPE", string.Empty));

            if (req.ContentLength > 0)
            {
                environmentVariables.Add(("CONTENT_LENGTH", Convert.ToString(req.ContentLength, CultureInfo.InvariantCulture)));
            }

            environmentVariables.Add(("CONTENT_TYPE", req.ContentType));
            environmentVariables.Add(("GATEWAY_INTERFACE", Version));
            environmentVariables.Add(("X_MATCHED_ROUTE", originalRoute?.Route ?? string.Empty));


            environmentVariables.Add(("QUERY_STRING", req.QueryString.HasValue ? req.QueryString.Value.Remove(0, 1) : string.Empty));
            environmentVariables.Add(("REMOTE_ADDR", this.context.Connection.RemoteIpAddress?.ToString()));
            environmentVariables.Add(("REMOTE_HOST", this.context.Connection.RemoteIpAddress?.ToString()));

            // TODO: set Remote User
            environmentVariables.Add(("REMOTE_USER", string.Empty));
            environmentVariables.Add(("REQUEST_METHOD", req.Method));
            environmentVariables.Add(("SCRIPT_NAME", originalRoute?.Route.TrimEnd('.').TrimEnd('/') ?? "/"));
            environmentVariables.Add(("SERVER_NAME", req.Host.Host));
            environmentVariables.Add(("SERVER_PORT", Convert.ToString(req.Host.Port ?? 80, CultureInfo.InvariantCulture)));
            environmentVariables.Add(("SERVER_PROTOCOL", req.Protocol));
            environmentVariables.Add(("SERVER_SOFTWARE", ServerVersion));

            var pathInfo = "";
            var routePrefix = originalRoute.Route;
            if (routePrefix.EndsWith("/...", StringComparison.InvariantCulture))
            {
                routePrefix = routePrefix.TrimEnd('.').TrimEnd('/');
                pathInfo = req.Path.Value.Length >= routePrefix.Length ? req.Path.Value.Remove(0, routePrefix.Length) : string.Empty;
            }

            this.pathInfo = pathInfo;
            environmentVariables.Add(("PATH_INFO", pathInfo));
            environmentVariables.Add(("PATH_TRANSLATED", pathInfo));

            var rawPathInfo = UrlEncode(pathInfo);
            environmentVariables.Add(("X_RAW_PATH_INFO", rawPathInfo));
            environmentVariables.Add(("X_FULL_URL", $"{req.Scheme}://{req.Host}{routePrefix}{rawPathInfo}{req.QueryString}"));

            foreach (var header in headers)
            {
                var key = $"HTTP_{header.Key.Replace("-", "_", StringComparison.InvariantCultureIgnoreCase)}";
                if (key.ToUpperInvariant() == "HTTP_AUTHORIZATION" || key.ToUpperInvariant() == "HTTP_CONNECTION")
                {
                    continue;
                }

                environmentVariables.Add((key, header.Value.ToString()));
            }

            return environmentVariables;
        }

        private string[] GetArgs()
        {
            string[] args = { this.pathInfo };
            return args.Concat(this.context.Request.QueryString.Value.Length > 0 ? this.context.Request.QueryString.Value.TrimStart('?').Split("&") : Array.Empty<string>())
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
            var statusCode = 200;
            var contentType = string.Empty;
            var reason = string.Empty;
            using var stdoutStream = new FileStream(outputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var headerBuffer = new byte[1024];
            var inputBuffer = new byte[1024];
            var lastByte = 0;
            var streamPosition = 0;
            var bytesRead = 0;
            var moreHeaders = true;
            var headerBufferPosition = 0;

            // Find the headers.
            while ((bytesRead = await stdoutStream.ReadAsync(inputBuffer.AsMemory(0, inputBuffer.Length))) != 0 && moreHeaders)
            {
                for (var i = 0; i < bytesRead; i++)
                {
                    streamPosition++;
                    if (inputBuffer[i] == 10)
                    {
                        // two newlines in a row, means headers are finished and the rest of the output (if any) is the response body.

                        if (lastByte == 10)
                        {
                            moreHeaders = false;
                            break;
                        }

                        // Remove any CR

                        var header = Encoding.UTF8.GetString(headerBuffer, 0, headerBufferPosition).TrimEnd();

                        // The header string might now be empty, if so skip it.

                        if (header.Length > 0)
                        {
                            headers.Add(header);
                        }

                        headerBufferPosition = 0;
                    }
                    else
                    {
                        if (headerBufferPosition == headerBuffer.Length)
                        {
                            throw new InvalidOperationException("Response Header too long");
                        }

                        // Ignore null characters.

                        if (inputBuffer[i] != 0)
                        {
                            headerBuffer[headerBufferPosition++] = inputBuffer[i];
                        }
                    }
                    lastByte = inputBuffer[i];
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
                                logger.TraceMessage($"Ignoring Reason {reason}");
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
                throw new InvalidOperationException("Module did not produce either location or content-type headers");
            }

            if (contentType.Length > 0)
            {
                this.context.Response.ContentType = contentType;
            }

            this.context.Response.StatusCode = statusCode;

            if (streamPosition < stdoutStream.Length)
            {
                stdoutStream.Seek(streamPosition, SeekOrigin.Begin);
                await stdoutStream.CopyToAsync(this.context.Response.Body).ConfigureAwait(false);
            }
        }

        private void AddHeader(string key, string value) => this.context.Response.Headers.TryAdd(key, value);
    }
}
