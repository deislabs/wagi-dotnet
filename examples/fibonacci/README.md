# Fibonacci WAGI Module

This example exposes a simple fibonacci WAGI function written in assembly script as a route endpoint in ASP.Net Core.

Clone the repo, switch to the examples/fibonacci folder and then run:

``` Console
dotnet run
```

This starts a ASP.Net Core Web application WAGI host on port 8888.

Use a browser or a tool like `curl` to test:

``` Console
$ curl -v http://localhost:8888/fibonacci?93
*   Trying 127.0.0.1...
* TCP_NODELAY set
* Connected to localhost (127.0.0.1) port 8888 (#0)
> GET /fibonacci?93 HTTP/1.1
> Host: localhost:8888
> User-Agent: curl/7.58.0
> Accept: */*
>
< HTTP/1.1 200 OK
< Date: Wed, 05 May 2021 10:49:29 GMT
< Content-Type: text/plain
< Server: Kestrel
< Transfer-Encoding: chunked
<
fib(93)=12200160415121876738
* Connection #0 to host localhost left intact
```

The configuration for this is example can be found in the [appsettings.Development.json](appsettings.Development.json) configuration file:

``` json
  // The name of the configuration section for the WASM route handler, by default this is expected to be called WASM.
  "WASM": {
    // The relative path to the directory where WASM modules defined in this configuration section are located.
    "ModulePath": "modules",
    // A dictionary of one or more modules to be exposed by the application
    "Modules": {
      // The path at which to expose the module
      "/fibonacci": {
        // The file name of the module.
        "FileName": "fibonacci.wasm"
      }
    }
  }

```
