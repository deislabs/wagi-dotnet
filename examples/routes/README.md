# Routes and Subroutes WAGI Module

This example exposes a simple fibonacci WAGI function written in assembly script as a route endpoint in ASP.Net Core.

Clone the repo, switch to the examples/fibonacci folder and then run:

``` Console
dotnet run
```

This starts a ASP.Net Core Web application WAGI host on port 8888.

Use a browser or a tool like `curl` to test:

``` Console
$ curl -v http://localhost:8888/example/goodbye
*   Trying 127.0.0.1:8888...
* Connected to localhost (127.0.0.1) port 8888 (#0)
> GET /example/goodbye HTTP/1.1
> Host: localhost:8888
> User-Agent: curl/7.79.1
> Accept: */*
>
* Mark bundle as not supporting multiuse
< HTTP/1.1 200 OK
< Date: Tue, 29 Mar 2022 15:00:20 GMT
< Content-Type: text/plain
< Server: Kestrel
< Transfer-Encoding: chunked
<
 Goodbye
* Connection #0 to host localhost left intact
```

Note that the route example/goodbye is not defined in the WAGI configuration file below, it is defined in the WASM module.

The configuration for this is example can be found in the [appsettings.Development.json](appsettings.Development.json) configuration file:

``` json
  // The name of the configuration section for the WAGI route handler, by default this is expected to be called Wagi.
  "Wagi": {
    // The relative path to the directory where WAGI modules defined in this configuration section are located.
    "ModulePath": "modules",
    // A dictionary of one or more modules to be exposed by the application
    "Modules": {
       // The logical name of the module definition
      "fibonacci": {
        // The file name of the module.
        "FileName":  "hello_wagi.wasm",
        // Route that is appended to the url of the server to form the URL to access the module
        "Route" : "/example"
      }
    }
  }

```
