# Modules.toml

This example shows how to use WAGI's modules.toml configuration file to configure endpoints. It uses [Wagi Fileserver](https://github.com/deislabs/wagi-fileserver)

Clone the repo, switch to the examples/modules.tomlcl folder and then run:

``` Console
dotnet run
```

This starts a ASP.Net Core Web application WAGI host on port 8888.

Use a browser or a tool like `curl` to test:

``` Console
$  curl -v http://localhost:8888/static/README.md
*   Trying ::1...
* TCP_NODELAY set
* Connected to localhost (::1) port 8888 (#0)
> GET /static/README.md HTTP/1.1
> Host: localhost:8888
> User-Agent: curl/7.55.1
> Accept: */*
>
< HTTP/1.1 200 OK
< Date: Wed, 18 Aug 2021 09:36:26 GMT
< Content-Type: text/plain
< Server: Kestrel
< Transfer-Encoding: chunked
<
# Modules.toml example
* Connection #0 to host localhost left intact
```

The configuration for this is example can be found in the [modules.toml](modules.toml) configuration file. 

Modules.toml configuration file specification can be found [here.](https://github.com/deislabs/wagi/blob/main/docs/configuring_and_running.md#the-modulestoml-configuration-file)