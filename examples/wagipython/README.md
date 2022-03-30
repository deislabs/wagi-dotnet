# Example of running Python-WASI as a WAGI Module

This example shows how to run [singlestore-labs python-wasi](https://github.com/singlestore-labs/python-wasi) as a WAGI Module.

The contents are in the folder wagi-python, the original source is [this GitHub repo](https://github.com/fermyon/wagi-python), full details in [this blog post.](https://www.fermyon.com/blog/python-wagi)

The configuration for running the module is in the `appsettings.json` file, it does not use `modules.toml` from `wagi-python`.

The example exposes python as a route endpoint in ASP.Net Core, the script that is run is specified in the argv property in the configuration, args are appended to the command passed to python.

To run the example clone the repo, switch to the local folder and then run:

``` Console
dotnet run
```

This starts a ASP.Net Core Web application WAGI host on port 8080.

Use a browser or a tool like `curl` to test:

```Console

 curl -v 'http://localhost:8080/?1&2&3&4'

 *   Trying 127.0.0.1:8080...
* TCP_NODELAY set
* Connected to localhost (127.0.0.1) port 8080 (#0)
> GET /?1&2&3&4 HTTP/1.1
> Host: localhost:8080
> User-Agent: curl/7.68.0
> Accept: */*
>
* Mark bundle as not supporting multiuse
< HTTP/1.1 200 OK
< Date: Mon, 28 Mar 2022 22:37:08 GMT
< Content-Type: text/plain; charset=UTF-8
< Server: Kestrel
< Transfer-Encoding: chunked
<
Hello from python on wasi

### Arguments ###

['/code/env.py', '1', '2', '3', '4']

### Env Vars ###

AUTH_TYPE:
CONTENT_TYPE:
GATEWAY_INTERFACE: CGI/1.1
HTTP_Accept: */*
HTTP_Host: localhost:8080
HTTP_User_Agent: curl/7.68.0
PATH_INFO:
PATH_TRANSLATED:
PYTHONHOME: /opt/wasi-python/lib/python3.11
PYTHONPATH: /opt/wasi-python/lib/python3.11
QUERY_STRING: 1&2&3&4
REMOTE_ADDR: 127.0.0.1
REMOTE_HOST: 127.0.0.1
REMOTE_USER:
REQUEST_METHOD: GET
SCRIPT_NAME:
SERVER_NAME: localhost
SERVER_PORT: 8080
SERVER_PROTOCOL: HTTP/1.1
SERVER_SOFTWARE: WAGI/1
X_FULL_URL: http://localhost:8080/?1&2&3&4
X_MATCHED_ROUTE: /
X_RAW_PATH_INFO:

### Files ###

env.py
* Connection #0 to host localhost left intact
```