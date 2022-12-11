# Example of making HTTP Requests from WAGI using wasi-experimental-http

This example shows WAGI Modules that use [wasi-experimental-http](https://github.com/deislabs/wasi-experimental-http) to make HTTP Requests.

There are 2 examples in the sample, one which exposes an entry point that forwards the data posted to it to the [Postman echo API](https://learning.postman.com/docs/developer/echo-api/), the other reads and writes to blobs in the Azure Storage Service.

This starts a ASP.Net Core Web application WAGI host on port 8888.

## Postman echo example

Clone the repo, switch to the examples/simplehttp folder and then run:

``` Console
dotnet run
```

This starts a ASP.Net Core Web application WAGI host on port 8888.

Use curl to post a request to http://localhost:8888/test, this will send a request to postman-echo.com:

```
curl -d "Test" http://localhost:8888/test -v
*   Trying 127.0.0.1...
* TCP_NODELAY set
* Connected to localhost (127.0.0.1) port 8888 (#0)
> POST /test HTTP/1.1
> Host: localhost:8888
> User-Agent: curl/7.58.0
> Accept: */*
> Content-Length: 4
> Content-Type: application/x-www-form-urlencoded
>
* upload completely sent off: 4 out of 4 bytes
< HTTP/1.1 200 OK
< Date: Thu, 10 Jun 2021 19:12:12 GMT
< Content-Type: application/json; charset=utf-8
< Server: Kestrel
< Transfer-Encoding: chunked
<
{"args":{},"data":"Test","files":{},"form":{},"headers":{"x-forwarded-proto":"https","x-forwarded-port":"443","host":"postman-echo.com","x-amzn-trace-id":"Root=1-60c2640c-58ffc97501ccd62c5f254dd0","content-length":"4","traceparent":"00-4671ab1d04d2e84c9ec1b5bd77d2fcef-062c6c2ca34ccc41-00","cookie":"sails.sid=s%3AiRsZ3Qzq2GsNrTQQxi3AhEELcyW7Hn6w.yRzXCs2iaMdX4LAyOafZ9QfhJyQp1Liu%2FJx3kllNjs8","content-type":"text/plain"},"json":null,"url":"https://postman-echo.com/post"}
```

## Azure blob example

The Azure blob example requires an [Azure account] (https://azure.microsoft.com/free). 

To create blobs in the Azure Storage service configure the WAGI Modules with details of the [Azure Storage Account](https://docs.microsoft.com/en-us/azure/storage/common/storage-account-create?tabs=azure-portal) to read and write to, modify [appsettings.Development.json](appsettings.Development.json) as follows:

```
 "Wagi": {
    "ModulePath": "modules",
    "Modules": {
      "Write blob": {
        "FileName": "blob.wasm",
        "Entrypoint": "writeblob",
        "Environment" :{
          "STORAGE_ACCOUNT":"", // STORAGE_ACCOUNT should be set to the name of the Azure storage account 
          "STORAGE_MASTER_KEY" : "" // STORAGE_MASTER_KEY should be set to one of the Azure storage account keys
        },
        "AllowedHosts": [
          "https://YOUR_STORAGE_ACCOUNT.blob.core.windows.net" // replace YOUR_STORAGE_ACCOUNT with the storage account name
        ],
        "HttpMethod": "post",
        "Route" : "/writeblob"
      },
      "Read blob": {
        "FileName": "blob.wasm",
        "Entrypoint": "readblob",
        "Environment" :{
          "STORAGE_ACCOUNT":"",// STORAGE_ACCOUNT should be set to the name of the Azure storage account 
          "STORAGE_MASTER_KEY" : "" // STORAGE_MASTER_KEY should be set to one of the Azure storage account keys
        },
        "AllowedHosts": [
          "https://YOUR_STORAGE_ACCOUNT.blob.core.windows.net" // replace YOUR_STORAGE_ACCOUNT with the storage 
        ],
        "Route" : "/readblob"
      }
    }
```

Now run:

``` Console
dotnet run
```

This starts a ASP.Net Core Web application WAGI host on port 8888.

To create or update a blob use curl to post a request to http://localhost:8888/writeblob?container=containername&blob=blobname. The container specified must already exist.

```
curl -d "Test blob write" 'http://localhost:8888/writeblob?container=wagitest&blob=test' -v
*   Trying 127.0.0.1...
* TCP_NODELAY set
* Connected to localhost (127.0.0.1) port 8888 (#0)
> POST /writeblob?container=wagitest&blob=test HTTP/1.1
> Host: localhost:8888
> User-Agent: curl/7.58.0
> Accept: */*
> Content-Length: 15
> Content-Type: application/x-www-form-urlencoded
>
* upload completely sent off: 15 out of 15 bytes
< HTTP/1.1 200 OK
< Date: Thu, 10 Jun 2021 19:38:06 GMT
< Content-Type: text/plain
< Server: Kestrel
< Transfer-Encoding: chunked
<
Writing 15 bytes.
```

To get the contents of an existing blob use curl to post a request to http://localhost:8888/readblob?container=containername&blob=blobname.

```
 curl 'http://localhost:8888/readblob?container=wagitest&blob=test' -v
*   Trying 127.0.0.1...
* TCP_NODELAY set
* Connected to localhost (127.0.0.1) port 8888 (#0)
> GET /readblob?container=wagitest&blob=test HTTP/1.1
> Host: localhost:8888
> User-Agent: curl/7.58.0
> Accept: */*
>
< HTTP/1.1 200 OK
< Date: Thu, 10 Jun 2021 19:39:06 GMT
< Content-Type: text/plain
< Server: Kestrel
< Transfer-Encoding: chunked
<
Test blob write
```