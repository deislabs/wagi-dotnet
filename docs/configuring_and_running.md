# Getting Started with WAGI

This guide covers configuring and running WAGI in ASP.Net Core , as well as loading a WebAssembly module.
It assumes you have already created an application and [installed](installation.md) the WAGI package.

This guide begins with starting the server, then covers the configuration settings.

## Running the server

Once the extension has been added to a ASP.Net application running is no different than any other ASP.Net application, for example:

```console
$ dotnet run
Building...
fail: Deislabs.WAGI.Extensions[0]
      No configuration found in section WASM
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /tmp/wagiproj
```

Notice that there is an error message produced as the extension is not yet configured to serve any WAGI module requests. In order to do this the extension must be configured as described below, note that any changes to configuration are not dynamially applied, you need to restart the application for changes to be picked up.

## Configuration

The extension is driven by configuration which can be provided using any [dotnet configuration provider](https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration). The following examples assume that a json file is being used.

Example Configuration:

``` json
 "Wagi": {
    "CacheConfigPath" : "cache.toml",
    "ModulePath": "modules",
    "MaxHttpRequests": 20,
    "BindleServer" : "https://my.bindle.server/v1",
    "Modules": {
      "MyModule": {
        "FileName": "filename",
        "Entrypoint": "entrypoint",
        "Volumes": {
          "/path/inside/wasm":"/path/inside/host"
        },
        "Environment" :{
          "ENVAR_NAME":"VALUE"
        },
        "Route" : "/path",
        "Hostnames" : [
          "127.0.0.1:5004",
          "127.0.0.1:5005"
        ],
        "HttpMethod" : "POST",
        "Authorize": true,
        "Roles" :{
          "Rolename"
        },
        "Policy" :{
          "Policyname"
        },
        "AllowedHosts" :{
          "https://host.example.com"
        },
        "MaxHttpRequests": 50
      },
      "rubywasm": {
        "FileName": "ruby.wasm",
        "Volumes": {
          "/": "wagi-ruby/lib",
          "/usr": "wagi-ruby/ruby-wasm32-wasi/usr"
        },
          "Route": "/",
          "argv": "ruby -v /env.rb ${ARGS}"
      }
    }
    "Bindles" :{
      "MyBindle" :{
        "Name": "example.bindles/myapp/1.0.0",
          "Route" : "/v1",
          "Hostnames" : [
            "127.0.0.1:5003"
          ]
      }
    }
  }
```

Configuration for the extension is defined in a configuration section which is named `Wagi` by default, any valid name can be used for this section, if you use a non default name then you should pass the section name to the ``AddWagi``` extension method.

- Fields
  - `CacheConfigPath`: The path to a wasmtime cache configuration file see [here](#enabling-caching) for details. 
  - `ModulePath`: The path to the directory on disk where the WASM modules are located.
  - `MaxHttpRequests`: Sets the maximum number of HTTP Requests a module can make using [wasi-experimental-http](https://github.com/deislabs/wasi-experimental-http). This value can be overridden in `ModuleDetails`, if not present defaults to 10, must be a value between 1 and 500.
  - `BindleServer`:  The address of the bindle server to be used to resolve any `Bindle` definitions.
  - `Modules` : Modules is a key value pair object where each item defines a WAGI module to be exposed by the server. The *key* is a logical name for the module and the *value* is a Module object. 
  - Module Object Fields
    - `Route`  (REQUIRED): The route path to expose the module on. The path pattern is applied to each address that the server is listening on (e.g. an item with the key`/path` translates to the `http://localhost:5000/path` and `https://localhost:5001/path` for a default server configuration. Unless the `Hostnames` field is specified.)
    - `Filename` (REQUIRED): The path relative to `ModulePath` of the module on the file system, the file should be named either `<name>.wat` for modules in Web Assembly Text format or `<name>.wasm` for binary modules.
    - `Environment`: Key value pairs of strings where the key is an environment variable name and the value is an environment variable value. Each entry respresents an Environment Variables created in the modules environment at runtime.
    - `Entrypoint` (default: `_start`): The name of the function within the module. This will directly execute that function. Most WASM/WASI implementations create a `_start` function by default. An example of a module that declares 3 entrypoints can be found [here](https://github.com/technosophos/hello-wagi).
    - `Volumes`: Key value pairs of strings where the key is an path in the WASM module and the value is a path in the host environment. Each entry respresents a host directory that is made available to the module at runtime.
    - `HttpMethod` (default: `GET`): the HTTP method for requests to be mapped. Can be either GET or POST.
    - `Hostnames` : A list of hostnames to expose the module on, this can include a port e.g `localhost:8080`. If no hostnames are specified then the module will be mapped to each url endpoint that the server is listening on.
    - `Authorize` (default: `false`): specifies that the module should only be accessible to authenticated users.
    - `Roles` : An array of roles that the user must belong to in order to access the module.
    - `Policies` : An array of policies that the user must satisfy to in order to access the module.
    - `AllowedHosts` : An array of hostnames that a module using [wasi-experimental-http](https://github.com/deislabs/wasi-experimental-http) can make, only hostnames in this array can be accessed by the module.
    - `MaxHttpRequests`: Sets the maximum number of HTTP Requests this module can make using [wasi-experimental-http](https://github.com/deislabs/wasi-experimental-http). If not present defaults to `MaxHttpRequests` specified in `WASM` configuration, must be a value between 1 and 500.
    - `Argv` allows the control of the Args passed to the module. The placeholders ${script_name} and ${args} are replaced with the path query string arguments. For example if the value of this configuration item is "ruby ${script_name} ${args}" then the url `/ruby/hello.rb?name=world` would result in argv passed to the module being `ruby hello.rb name=world`. 
  - `Bindles` : Bindles is a key value pair object where each item defines a [bindle](https://github.com/deislabs/bindle) hosted at `BindleServer`  to be exposed by the server. Like the `Modules` property the *key* is a logical name for the bindle and the *value* is a Bindle Object.
    - Bindle Object Fields
      - `Name` (REQUIRED): The Name of the bindle to be loaded from the bindle server.
      - `Route`  (REQUIRED): The route path to expose the bindle on. The path pattern is applied to each address that the server is listening on (e.g. an item with the key`/path` translates to the `http://localhost:5000/path` and `https://localhost:5001/path` for a default server configuration. Unless the `Hostnames` field is specified.)
      - `Hostnames` : A list of hostnames to expose the bindle on, this can include a port e.g `localhost:8080`. If no hostnames are specified then the module will be mapped to each url endpoint that the server is listening on.
      - `Environment`: Key value pairs of strings where the key is an environment variable name and the value is an environment variable value. Each entry respresents an Environment Variable created in the environment at runtime for each module in the bindle.  

Here is a brief example of a configuration file that declares two routes:

```json
 "Wagi": {
    "ModulePath": "modules",
    "Modules": {
      "hello": {
        "FileName": "hello.wasm",
        "Route": "/hello",
      },
      "goodbye": {
        "FileName": "goodbye.wasm",
        "Route": "/goodbye",
      }
    }
  }

```

Each key-value pair in the `modules` property of the config is responsible for mapping a route (the path part of a URL) to an executable piece of code.

The two required directives for a module section are:

- `Route`: The path-pattern of a URL
- `FileName`: The file name of the WebAssembly module to execute

Routes are paths relative to the all the addresses defined in endpoints that the server is listening on. Assuming the routes above are running on a server that has an endpoint defined with the URL of `http://example.com`:

- The `/` `Route` would handle traffic to `http://example.com/` 
- A `Route` like `/hello` would handle traffic to `http://example.com/hello`
- A `Route` like `/hello` would handle traffic to `http://example.com/hello`

The `FileName` property is the name of a `wasm` or `wat` file on the filesystem. The filename is combined with the value of `ModulePath` and will be resolved relative to the current working directory in which the application was started.

Routes can be restricted to a subset of the addresses that the server is listening on by specifying the `Hostnames` property:

```json
 "Wagi": {
    "ModulePath": "modules",
    "Modules": {
      "hello": {
        "FileName": "hello.wasm",
        "Route": "/hello",
        "Hostnames": ["localhost:8080"]
      },
      "goodbye": {
        "FileName": "goodbye.wasm",
        "Route": "/goodbye",
      }
    }
  }

```
In this case the `hello` module would only be accessible at the URL http://localhost:8080/hello. (Assuming that the server is listening using HTTP)

### Volume Mounting

In addition to the required directives, the configurations sections support several other properties.
One of these is the `Volume` property, which specifies one or more host directories to be mounted as a local directory into the module.

By default, Wasm modules in WAGI have no ability to access the host filesystem.
That is, a Wasm module cannot open `/etc/` and read the files there, even if the application server has access to `/etc/`.
In WAGI, modules are considered untrusted when it comes to accessing resources on the host.
But it is definitely the case that code sometimes needs access to files.

Here is an example of providing a volume:

```json
 "Wagi": {
    "ModulePath": "modules",
    "Modules": {
      "Access Files": {
        "FileName": "bar.wasm",
        "Route": "/path",
        "Volumes": {
          "/path/inside/wasm":"/path/on/host"
        }
      }
    }
  }
```

In this case, the `Volumes` property tells WAGI to expose the contents of `/path/on/host` to the `bar.wasm` module.
But `bar.wasm` will see that directory as `/path/inside/wasm`. Importantly, it will not be able to access any other parts of the filesystem. For example, it will not see anything on the path `/path/inside`. It _only_ has access to the paths specified
in the `Volumes` property.

#### Environment Variables

Similarly to volumes, by default a WebAssembly module cannot access the host's environment variables.
However, the environment property provides a way for you to pass in environment variables:

```json
 "Wagi": {
    "ModulePath": "modules",
    "Modules": {
      "Env Vars Example": {
        "FileName": "hello.wasm",
        "Route": "/path",
        "Environment" :{
          "TEST_NAME":"test value"
        }
      }
    }
  }
```

In this case, the environment variable `TEST_NAME` will be set to `test value` for the `hello.wasm` module.
When the module starts up, it will be able to access the `TEST_NAME` variable.

Note that while the module will not be able to access the host environment variables, WAGI does provide a wealth of other environment variables. See [Environment Variables](environment_variables.md) for details.

#### Entrypoint

By default, a WASM WASI module has a function called `_start()`.
Usually, this function is created at compilation time, and typically it just calls the `main()`
function (this is a detail specific to the language in which the code was written).

Sometimes, though, you may want to have WAGI invoke another function.
This is what the `entrypoint` property is for.

The following example shows loading the same module at three different paths, each time
invoking a different function:

```json
"Wagi": {
    "ModulePath": "modules",
    "Modules": {
      "hello": {
        "FileName": "bar.wasm",
        "Route": "/hello",
        // With no `entrypoint`, this will invoke `_start()`
      },
      "Entrypoint hello": {
        "FileName": "bar.wasm",
        "Route" : "/entrypoint/hello",
        "Entrypoint": "hello"
        // Executes the `hello()` function in the module (instead of `_start`)
      },
      "Entrypoint Goodbye": {
        "FileName": "bar.wasm",
        "Route": "/entrypoint/goodbye",
        "Entrypoint": "goodbye"
        // Executes the `goodbye()` function in the module (instead of `_start`)
      }
    }
  }
```

### Authorization

By adding additional properties to a modules configuration runtime access can be controlled using ASP.Net Core [Authentication] (<https://docs.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-5.0>) and [Authorization](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/introduction?view=aspnetcore-5.0) capabailities. Any registered authentication scheme can be used, the configuration only specifies the conditions that should be met to allow access not how those conditions are fulfilled.

The simplest example is to require that a user is authenticated to access the module, the following example shows how to configure a module to require that all users that access it be authnticated.

```json
"Wagi": {
    "ModulePath": "modules",
    "Modules": {
      "Hellowat with auth": {
        "FileName": "hello.wat",
        "Route": "/hellowatauth",
        "Authorize" : true
      }
    }
}
```

With this configuration access to the module will only be granted to logged in users.

It is also possible to require that the logged in user is a member of one or more roles, for example:

```json
"Wagi": {
    "ModulePath": "modules",
    "Modules": {
      "Hellowat with role": {
        "FileName": "hello.wat",
        "Route": "/hellowatrole",
        "Roles" : ["admin"]
      },
    }
}
```

With this configuration the logged in user must be assigned the ```admin``` role.

It is also possible to require that the logged in user satisfies one or more [policies](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-5.0) in order to access the module:

```json
"Wagi": {
    "ModulePath": "modules",
    "Modules": {
      "Hellowat with Policy": {
        "FileName": "hello.wat",
        "Route": "/hellowatpolicy",
        "Policies" : ["IsASuperAdmin"]
      },
    }
}
```

In this example the the logged in user must be satisfy the ```IsASuperAdmin``` policiy.

To enforce authorization on modules requires that ASP.Net is configured correctly, a simple example showing how to do this can be found [here](../examples/watmwithauth).

## Making HTTP Requests from Modules

Modules can make HTTP Requests using the [wasi-experimental-http](https://github.com/deislabs/wasi-experimental-http) API. To use this the modules configuration must provide an array of hosts that the module commuicates with , without this modules will be unable to make requests, in addition a modules configuration may override the maximum number of requests that an instance of the module can make:

```json
"Wagi": {
    "ModulePath": "modules",
    "Modules": {
      "Outbound HTTP": {
        "FileName": "optimized.wasm",
        "Route": "/test",
        "AllowedHosts": [
          "https://postman-echo.com/"
        ],
        "HttpMethod": "post",
        "MaxHTTPRequests": 1
      },
    }
}
```

Examples showing how to do this can be found [here](../examples/simplehttp). 
The postman example is written in AssemblyScript and can be found [here] (https://github.com/simongdavies/http-wagi-as).
The Azure example is written in Rust and can be found [here] (https://github.com/simongdavies/http-azure-rust).

## Enabling Caching

To enable the [Wasmtime cache](https://docs.wasmtime.dev/cli-cache.html), which caches the result of the compilation
of a WebAssembly module, resulting in improved instantiation times for modules, you can create a `cache.toml` file
with the following structure:

```toml
[cache]
enabled = true
directory = "<absolute-path-to-a-cache-directory>"
# optional
# see more details at https://docs.wasmtime.dev/cli-cache.html
cleanup-interval = "1d"
files-total-size-soft-limit = "10Gi"
```

Then update the configuration to use it:

```json
"Wagi": {
    "CacheConfigPath" : "cache.toml",
    "ModulePath": "modules",
    "Modules": {
      "Caching": {
        "FileName": "test.wasm",
        "Route": "/test",
      }
    }
}
```

## Using Bindle 

To load modules defined as bindle create one or more bindle configuration entries, each entry contains the URL to a Bindle server and the name of the bindle containing the modules to configure:

```
"Wagi": {
  "ModulePath": "modules",
  "BindleServer" : "https://some.bindleserver.com/v1",
  "Bindles": {
    "My Bindle App": {
      "Route":"/",
      "Name": "example.bindles/myapp/1.0.0"
    }
  }
}
```

In the above example when the `MapWASMModules` extension method is called it will retrieve details of any WASM/WAGI Modules contained in the bindle at the`BindleServer` URL and will download the modules and any associated artefacts, it will then configure modules and routes as defined in the bindle, each route will be a child path of the path in the key for this item.

Simlarly to local modules, bindle modules Routes can be restricted to a subset of the addresses that the server is listening on by specifying the `Hostnames` property:

```json
"Wagi": {
  "ModulePath": "modules",
  "BindleServer" : "https://some.bindleserver.com/v1",
  "Bindles": {
    "My Bindle App": {
      "Route":"/",
      "Name": "example.bindles/myapp/1.0.0",
      "Hostnames": ["localhost:8080"]
    }
  }
}
```
In this case the modules in the bindle would only be accessible at the URL http://localhost:8080/. (Assuming that the server is listening using HTTP)

## Using modules.toml 

A Wagi [modules.toml](https://github.com/deislabs/wagi/blob/main/docs/configuring_and_running.md#the-modulestoml-configuration-file) configuration file can also be used to configure the modules that are loaded by the server. To do this use the extension Method `AddModulesTomlFile` in namespace `Deislabs.Wagi.Configuration.Modules.Toml` e.g:

```csharp

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            }).ConfigureAppConfiguration(builder =>
            {
                builder.AddModulesTomlFile("modules.toml", false, true);
            });

```

Note that using modules.toml requires that the configuration section is called Wagi ,calling AddWagi with a different section name will not work. Also note that whilst modules.toml support is provided by a custom configuration provider that provider only supports modules.toml and not any other configuration.
## What's Next?

Next, read about [Writing Modules](writing_modules.md) for WAGI.
