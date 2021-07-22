# WATM Hello World! WAGI Module

This example exposes a hello world WAGI function written in Web Assembly Text format as a route endpoint in ASP.Net Core.

Clone the repo, switch to the examples/watm folder and then run:

``` Console
dotnet run
```

This starts a ASP.Net Core Web application WAGI host on port 8888.

Use a browser or a tool like `curl` to test:

``` Console
$ curl -v http://localhost:8888/hellowat
*   Trying 127.0.0.1...
* TCP_NODELAY set
* Connected to localhost (127.0.0.1) port 8888 (#0)
> GET /hellowat HTTP/1.1
> Host: localhost:8888
> User-Agent: curl/7.58.0
> Accept: */*
>
< HTTP/1.1 200 OK
< Date: Wed, 05 May 2021 12:14:39 GMT
< Content-Type: text/plain
< Server: Kestrel
< Transfer-Encoding: chunked
<
Hello World!
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
    // The logical name of the module definition
      "hellowat": {
        // The file name of the module.
        "FileName": "hello.wat",
        // Route that is appended to the url of the server to form the URL to access the module
        "Route" : "/hellowat"
      }
    }
  }

```
