# Using Bindle to serve WAGI Modules

This example shows how to use [Bindle](https://github.com/deislabs/bindle) to host Wagi modules and serve them in wagi-dotnet.

The example is configured to load three bindles from a bindle server located at `https://bindle.deislabs.io/v1`. Each bindle contains a single WASM module with a single hello world type function.

Clone the repo, switch to the examples/bindlesource folder and then run:

``` Console
dotnet run
```

This starts a ASP.Net Core Web application WAGI host on port 8888.

Use a browser or a tool like `curl` to test:

### Example 1

``` Console
$ curl -v http://localhost:8888/
*   Trying 127.0.0.1...
* TCP_NODELAY set
* Connected to localhost (127.0.0.1) port 8888 (#0)
> GET / HTTP/1.1
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
Hello, world from 1.0.0!
* Connection #0 to host localhost left intact
```

### Example 2

``` Console
$ curl -v http://localhost:8888/v1
*   Trying 127.0.0.1...
* TCP_NODELAY set
* Connected to localhost (127.0.0.1) port 8888 (#0)
> GET /v1 HTTP/1.1
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
Kia ora, world from 1.1.0!
* Connection #0 to host localhost left intact
```

### Example 3

``` Console
$ curl -v http://localhost:8888/1.1.0
*   Trying 127.0.0.1...
* TCP_NODELAY set
* Connected to localhost (127.0.0.1) port 8888 (#0)
> GET /1.1.0 HTTP/1.1
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
Kia ora, world from 1.1.0!
* Connection #0 to host localhost left intact
```

The configuration for this is example can be found in the [appsettings.Development.json](appsettings.Development.json) configuration file:

``` json
  // The name of the configuration section for the WASM route handler, by default this is expected to be called WASM.
  "WASM": {
    // The relative path to the directory where WASM modules defined in this configuration section are to be downloaded to.
    "ModulePath": "modules",
    // A dictionary of one or more bindles to be exposed by the application
    "Bindles": {
      // The path at which to expose the modules contained in the bindle
      "/": {
        // The Url of the bindleserver.
        "BindleUrl": "https://bindle.deislabs.io/v1",
        // The name of the bindle that contains the modules to be loaded.
        "Name": "hippos.rocks/helloworld/1.1.0"
      }
    }
  }

```
