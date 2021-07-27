# Wildcard Example

This example shows how to use Wildcard routing.

Any route ending with `/...` is treated as a wildcard route. A wildcard route will receive all requests for paths which match the prefix, for example the route `/path/...` will match 
`/path/other` and `/path/someother` in the case where there are multiple routes which match a path the most specific route will be chosen. 

In the example the routes `/path`, `/path/...` and `/...` are defined a and requests will match as follows:

| Path | Route|
|---|---|
| /path | /path |
| /path/other | /path/... |
| /path/some/other | /path/... |
| /some/other/path | /... |
| /someotherpath | /... |

To run the examples clone the repo, switch to the examples/wildcard directory and then run:

``` Console
dotnet run
```

This starts a dotnet Web application WAGI host on port 8888.

Use a browser or a tool like `curl` to test:

``` Console
$ curl  http://localhost:8888/path
```

The output will include the environment variables that were set for the module, the environment variable `X_MATCHED_ROUTE ` is set to match the value of the route chosen, so the value of this varibale in the output shows which route was matched.
In the case above the value should be `/path`.

The configuration for this example can be found in the [appsettings.Development.json](appsettings.Development.json) configuration file.
