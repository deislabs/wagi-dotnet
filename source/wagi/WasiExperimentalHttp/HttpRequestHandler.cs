namespace Wasi.Experimental.Http
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.IO;
  using System.Linq;
  using System.Net.Http;
  using System.Net.Http.Headers;
  using System.Text;
  using System.Threading;
  using Microsoft.Extensions.Logging;
  using Wasi.Experimental.Http.Exceptions;
  using Wasmtime;

  /// <summary>
  /// HttpRequestHandler provides support for wasi_experimental_http.
  /// </summary>
  internal class HttpRequestHandler : IDisposable
  {
    private const string ModuleName = "wasi_experimental_http";
    private const string MemoryName = "memory";
    private const int MaxResponses = 10;
    private const int OK = 0;
    private const int RuntimeError = 12;

    private readonly Dictionary<int, Response> responses;
    private readonly string[] allowedMethods = new string[] { "DELETE", "GET", "HEAD", "OPTIONS", "PATCH", "POST", "PUT", "TRACE" };
    private readonly ILogger logger;
    private readonly HttpClient httpClient;
    private readonly List<Uri> allowedHosts;

    private int lastResponse;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRequestHandler"/> class.
    /// </summary>
    /// <param name="host">The WASMTime host.</param>
    /// <param name="loggerFactory">ILoggerFactory.</param>
    /// <param name="httpClientFactory">IHttpClientFactory to be used for module Http Requests. </param>
    /// <param name="allowedHosts">A set of allowedHosts (hostnames) that the module can send HTTP requests to.</param>
    public HttpRequestHandler(Host host, ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, List<Uri> allowedHosts = null)
    {
      this.logger = loggerFactory.CreateLogger(typeof(HttpRequestHandler).FullName);
      this.httpClient = httpClientFactory.CreateClient();
      this.allowedHosts = allowedHosts;
      this.responses = new Dictionary<int, Response>();
      host.DefineFunction<Caller, int, int, int, int, int>(ModuleName, "body_read", this.ReadBody);
      host.DefineFunction<Caller, int, int>(ModuleName, "close", this.Close);
      host.DefineFunction<Caller, int, int, int, int, int, int, int, int, int, int, int>(ModuleName, "req", this.Request);
      host.DefineFunction<Caller, int, int, int, int, int, int, int>(ModuleName, "header_get", this.GetHeader);
      host.DefineFunction<Caller, int, int, int, int, int>(ModuleName, "headers_get_all", this.GetAllHeaders);
    }

#pragma warning disable CS1591
#pragma warning disable SA1600
    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (disposing)
      {
        this.httpClient?.Dispose();
        foreach (var response in this.responses)
        {
          response.Value.Dispose();
        }
      }
    }

#pragma warning restore CS1591
#pragma warning restore SA1600
    private static CallerMemory GetMemory(Caller caller)
    {
      var memory = caller.GetMemory(MemoryName);
      if (memory is null)
      {
        throw new MemoryNotFoundException();
      }

      return memory;
    }

    private int ReadBody(Caller caller, int handle, int bufferPtr, int bufferLength, int bufferWrittenPtr)
    {
      this.logger.LogTrace($"ReadBody called with handle {handle}");
      try
      {
        var memory = GetMemory(caller);
        var response = this.GetResponse(handle);
        var available = Math.Min(Convert.ToInt32(response.Content.Length) - Convert.ToInt32(response.Content.Position), bufferLength);
        response.Content.Read(memory.Span.Slice(bufferPtr, available));
        memory.WriteInt32(bufferWrittenPtr, available);
        return OK;
      }
      catch (ExperimentalHttpException ex)
      {
        return ex.ErrorCode;
      }
#pragma warning disable CA1031
      catch (Exception ex)
#pragma warning restore CA1031
      {
        this.logger.LogTrace($"Exception: {ex}");
        return RuntimeError;
      }
    }

    private Response GetResponse(int handle)
    {
      var response = this.responses[handle];
      if (response == null)
      {
        this.logger.LogTrace($"Failed to get response Handle: {handle}");
        throw new InvalidHandleException();
      }

      return response;
    }

    private int Close(Caller call, int handle)
    {
      {
        this.logger.LogTrace($"Function close was called  with handle {handle}");
        try
        {
          var response = this.GetResponse(handle);
          this.responses.Remove(handle);
          response.Dispose();
          return OK;
        }
        catch (ExperimentalHttpException ex)
        {
          return ex.ErrorCode;
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
          this.logger.LogTrace($"Exception: {ex}");
          return RuntimeError;
        }
      }
    }

    private int Request(Caller caller, int urlPtr, int urlLength, int methodPtr, int methodLength, int headersPtr, int headersLength, int bodyPtr, int bodyLength, int statusCodePtr, int handlePtr)
    {
      this.logger.LogTrace("Function req was called");
      try
      {
        var memory = GetMemory(caller);
        var url = this.ValidateHostAllowed(memory, urlPtr, urlLength);
        var method = this.ValidateMethod(memory, methodPtr, methodLength);
        var headers = this.GetHttpRequestHeaders(memory, headersPtr, headersLength);
        var body = this.GetRequestBody(memory, bodyPtr, bodyLength);
        var httpResponseMessage = this.SendHttpRequest(url, method, headers, body);
        memory.WriteInt32(statusCodePtr, (int)httpResponseMessage.StatusCode);
        var handle = Interlocked.Increment(ref this.lastResponse);
        if (handle > MaxResponses)
        {
          throw new TooManySessionsException();
        }

        var response = new Response(httpResponseMessage);
        this.responses.Add(handle, response);
        memory.WriteInt32(handlePtr, handle);
        this.logger.LogTrace($"Function req created handle {handle}");
        return OK;
      }
      catch (ExperimentalHttpException ex)
      {
        return ex.ErrorCode;
      }
#pragma warning disable CA1031
      catch (Exception ex)
#pragma warning restore CA1031
      {
        this.logger.LogTrace($"Exception: {ex}");
        return RuntimeError;
      }
    }

    private int GetHeader(Caller caller, int handle, int namePtr, int nameLength, int valuePtr, int valueLength, int valueWrittenPtr)
    {
      this.logger.LogTrace($"Function header_get was called with handle {handle}");
      try
      {
        var memory = GetMemory(caller);
        string headerName;
        try
        {
          headerName = memory.ReadString(namePtr, nameLength);
        }
        catch (Exception ex)
        {
          var message = $"Failed to read header  Exception: {ex.Message}";
          this.logger.LogTrace(message);
          throw new MemoryAccessException(message, ex);
        }

        this.logger.LogTrace($"header_get Header Name: {headerName}");
        var response = this.GetResponse(handle);

        HttpHeaders headers;
        if (headerName.StartsWith("content", true, CultureInfo.InvariantCulture))
        {
          headers = response.HttpResponseMessage.Content.Headers;
        }
        else
        {
          headers = response.HttpResponseMessage.Headers;
        }

        var headerValues = headers.Where(h => h.Key.ToUpperInvariant() == headerName.ToUpperInvariant()).Select(h => h.Value).FirstOrDefault();
        if (headerValues == null)
        {
          this.logger.LogTrace($"Failed to get Header {headerName}");
        }

        var headerValue = string.Join(';', headerValues);
        var headerValueLength = headerValue.Length;
        if (headerValueLength > valueLength)
        {
          var message = $"Header Value for {headerName} Too Big. Bufffer Length {valueLength} Header Length {headerValueLength}";
          this.logger.LogTrace(message);
          throw new BufferTooSmallException(message);
        }

        memory.WriteString(valuePtr, headerValue);
        memory.WriteInt32(valueWrittenPtr, headerValueLength);
        return OK;
      }
      catch (ExperimentalHttpException ex)
      {
        return ex.ErrorCode;
      }
#pragma warning disable CA1031
      catch (Exception ex)
#pragma warning restore CA1031
      {
        this.logger.LogTrace($"Exception: {ex}");
        return RuntimeError;
      }
    }

    private int GetAllHeaders(Caller caller, int handle, int bufferPtr, int bufferLength, int bufferWrittenPtr)
    {
      this.logger.LogTrace($"Function headers_get_all was called with handle {handle}");
      try
      {
        var memory = GetMemory(caller);
        var response = this.GetResponse(handle);
        var allHeaders = new StringBuilder();

        foreach (var header in response.HttpResponseMessage.Headers)
        {
          allHeaders.AppendLine($"{header.Key}:{string.Join(';', header.Value)}");
        }

        foreach (var header in response.HttpResponseMessage.Content.Headers)
        {
          allHeaders.AppendLine($"{header.Key}:{string.Join(';', header.Value)}");
        }

        var headerValuesLength = allHeaders.Length;
        if (headerValuesLength > bufferLength)
        {
          var message = $"Header Values for all header Too Big. Bufffer Length {bufferLength} Header Length {headerValuesLength}";
          this.logger.LogTrace(message);
          throw new BufferTooSmallException(message);
        }

        memory.WriteString(bufferPtr, allHeaders.ToString());
        memory.WriteInt32(bufferWrittenPtr, headerValuesLength);
        return OK;
      }
      catch (ExperimentalHttpException ex)
      {
        return ex.ErrorCode;
      }
#pragma warning disable CA1031
      catch (Exception ex)
#pragma warning restore CA1031
      {
        this.logger.LogTrace($"Exception: {ex}");
        return RuntimeError;
      }
    }

    private string ValidateHostAllowed(CallerMemory memory, int urlPtr, int urlLength)
    {
      string url;
      try
      {
        url = memory.ReadString(urlPtr, urlLength);
      }
      catch (Exception ex)
      {
        var message = $"Failed to read url Exception: {ex.Message}";
        this.logger.LogTrace(message);
        throw new MemoryAccessException(message, ex);
      }

      if (string.IsNullOrEmpty(url))
      {
        this.logger.LogTrace("Request Url is missing");
        throw new InvalidUrlException();
      }

      this.logger.LogTrace($"Request URL: {url}");

      if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
      {
        this.logger.LogTrace($"Url {url} is invalid");
        throw new InvalidUrlException();
      }

      if (this.allowedHosts != null && !this.allowedHosts.Select(a => a.Host.ToUpperInvariant() == uri.Host.ToUpperInvariant()).Any())
      {
        this.logger.LogTrace($"host {uri.Host} not allowed");
        throw new DestinationNotAllowedException();
      }

      return url;
    }

    private string ValidateMethod(CallerMemory memory, int methodPtr, int methodLength)
    {
      string method;
      try
      {
        method = memory.ReadString(methodPtr, methodLength);
      }
      catch (Exception ex)
      {
        var message = $"Failed to read method Exception: {ex.Message}";
        this.logger.LogTrace(message);
        throw new MemoryAccessException(message, ex);
      }

      if (string.IsNullOrEmpty(method))
      {
        this.logger.LogTrace("Request Method is missing");
        throw new InvalidMethodException();
      }

      if (!this.allowedMethods.Contains(method.ToUpperInvariant()))
      {
        this.logger.LogTrace($"Request Method {method} is not allowed");
        throw new InvalidMethodException();
      }

      this.logger.LogTrace($"Request Method: {method}");
      return method;
    }

    private Dictionary<string, string> GetHttpRequestHeaders(CallerMemory memory, int headersPtr, int headersLength)
    {
      var headers = new Dictionary<string, string>();
      string headersAsString;
      try
      {
        headersAsString = memory.ReadString(headersPtr, headersLength);
      }
      catch (Exception ex)
      {
        var message = $"Failed to read headers Exception: {ex.Message}";
        this.logger.LogTrace(message);
        throw new MemoryAccessException(message, ex);
      }

      if (string.IsNullOrEmpty(headersAsString))
      {
        this.logger.LogTrace($"No Request Headers Provided");
        return headers;
      }

      using var stringReader = new StringReader(headersAsString);
      var line = string.Empty;
      while ((line = stringReader.ReadLine()) != null)
      {
        var index = line.IndexOf(':', StringComparison.InvariantCultureIgnoreCase);
        var name = line.Substring(0, index);
        var value = line[++index..];
        this.logger.LogTrace($"Adding Header {name}");
        headers.Add(name, value);
      }

      return headers;
    }

    private byte[] GetRequestBody(CallerMemory memory, int bodyPtr, int bodyLength)
    {
      byte[] body;
      try
      {
        body = memory.Span.Slice(bodyPtr, bodyLength).ToArray();
      }
      catch (Exception ex)
      {
        var message = $"Failed to get request body Exception: {ex.Message}";
        this.logger.LogTrace(message);
        throw new MemoryAccessException(message, ex);
      }

      return body;
    }

    private HttpResponseMessage SendHttpRequest(string url, string method, Dictionary<string, string> headers, byte[] body = null)
    {
      HttpResponseMessage httpResponseMessage = null;
      var httpMethod = new HttpMethod(method);
      using var req = new HttpRequestMessage(httpMethod, url);
      if (body != null && body.Length > 0)
      {
        req.Content = new ByteArrayContent(body);
      }

      var contentHeaders = headers.Where(h => h.Key.StartsWith("CONTENT", true, CultureInfo.InvariantCulture)).DefaultIfEmpty();

      foreach (var contentHeader in contentHeaders)
      {
        req.Content?.Headers.Add(contentHeader.Key, contentHeader.Value);
        headers.Remove(contentHeader.Key);
      }

      foreach (var header in headers)
      {
        try
        {
          req.Headers.Add(header.Key, header.Value.Split(';'));
        }
        catch (Exception ex)
        {
          var message = $"Failed to add HTTP Header {header.Key} Exception: {ex.Message}";
          this.logger.LogTrace(message);
          throw new InvalidEncodingException(message, ex);
        }
      }

      try
      {
        httpResponseMessage = this.httpClient.Send(req);
      }
      catch (Exception ex)
      {
        var message = $"Failed to make HTTP Request Exception: {ex.Message}";
        this.logger.LogTrace(message);
        throw new RequestException(message, ex);
      }

      return httpResponseMessage;
    }
  }
}
