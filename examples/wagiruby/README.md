# Example of running Ruby as a WAGI Module

This contents in wagi-ruby is a copy of the WAGI-Ruby example by [Fermyon](https://www.fermyon.com/) from [this GitHub repo.](https://github.com/fermyon/wagi-ruby.)

This example exposes a ruby script as a route endpoint in ASP.Net Core.

To run the example clone the repo, switch to the local folder and then run:

``` Console
dotnet run
```

This starts a ASP.Net Core Web application WAGI host on port 8080.

Use a browser or a tool like `curl` to test:

``` Console
$ curl -v http://127.0.0.1:8080?env.rb
*   Trying 127.0.0.1:8080...
* TCP_NODELAY set
* Connected to 127.0.0.1 (127.0.0.1) port 8080 (#0)
> GET /?env.rb HTTP/1.1
> Host: 127.0.0.1:8080
> User-Agent: curl/7.68.0
> Accept: */*
> 
* Mark bundle as not supporting multiuse
< HTTP/1.1 200 OK
< Date: Fri, 25 Feb 2022 21:32:47 GMT
< Content-Type: text/plain; charset=UTF-8
< Server: Kestrel
< Transfer-Encoding: chunked
< X-Foo-Header: Bar
< 
Hello from ruby!

ruby version: 3.2.0 (2022-02-10) [wasm32-wasi]

### Arguments ###


### Env Vars ###

AUTH_TYPE=
CONTENT_TYPE=
GATEWAY_INTERFACE=CGI/1.1
X_MATCHED_ROUTE=/
QUERY_STRING=env.rb
REMOTE_ADDR=127.0.0.1
REMOTE_HOST=127.0.0.1
REMOTE_USER=
REQUEST_METHOD=GET
SCRIPT_NAME=
SERVER_NAME=127.0.0.1
SERVER_PORT=8080
SERVER_PROTOCOL=HTTP/1.1
SERVER_SOFTWARE=WAGI/1
PATH_INFO=
PATH_TRANSLATED=
X_RAW_PATH_INFO=
X_FULL_URL=http://127.0.0.1:8080/?env.rb
HTTP_Accept=*/*
HTTP_Host=127.0.0.1:8080
HTTP_User_Agent=curl/7.68.0

### Files ###

.
..
env.rb
* Connection #0 to host 127.0.0.1 left intact
```

There is also a live version of the example at https://rubywasm.azurewebsites.net/?env.rb