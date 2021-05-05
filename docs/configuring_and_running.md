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

Notice that there is an error message produced as the extension is not yet configured to serve any WAGI module requests. In order to do this the extension must be configured as described below, note that any changes to configuration are not dynamially applied, you need to restart the application for changes to be picked up.

## Configuration

The extension is driven by configuration which can be provided using any [.NET configuration provider](https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration). The following examples assume that a json file is being used.

Example Configuration:

``` json
 "WASM": {
    "ModulePath": "modules",
    "Modules": {
      "/path": {
        "FileName": "filename",
        "Entrypoint": "entrypoint",
        "Volumes": {
          "/path/inside/wasm":"/path/inside/host"
        },
        "Environment" :{
          "ENVAR_NAME":"VALUE"
        },
        "HttpMethod" : "POST"
      }
    }
  }
```

Configuration for the extension is defined in a configuration section which is named `WASM` by default, any valid name can be used for this section, if you use a non default name then you should pass the section name to the ```MapWASMModules``` extension method.

- Fields
  - `ModulePath`: The path to the directory on disk where the WASM modules are located.
  - `Modules` : Modules is a object where the key is path pattern used to create a route to the module and the values is a Module object. The path pattern is applied to each address that the server is listening on (e.g. by default `/foo` becomes  `http://localhost:5000/foo` and `https://localhost:5001/foo`)
  - Module Fields
    - `filename` (REQUIRED): The path relative to `ModulePath` of the module on the file system, the file should be named either `<name>.wat` for modules in Web Assembly Text format or `<name>.wasm` for binary modules.
    - `environment`: Key value pairs of strings where the key is an environment variable name and the value is an environment variable value. Each entry respresents an Environment Variables created in the modules environment at runtime.
    - `entrypoint` (default: `_start`): The name of the function within the module. This will directly execute that function. Most WASM/WASI implementations create a `_start` function by default. An example of a module that declares 3 entrypoints can be found [here](https://github.com/technosophos/hello-wagi).
    - `volumes`: Key value pairs of strings where the key is an path in the WASM module and the value is a path in the host environment. Each entry respresents a host directory that is made available to the module at runtime.
    - `httpmethod` (default: `GET`): the HTTP method for requests to be mapped. Can be either GET or POST.

Here is a brief example of a configuration file that declares two routes:

```json
 "WASM": {
    "ModulePath": "modules",
    "Modules": {
      "/hello": {
        "FileName": "hello.wasm"
      },
      "/goodbye": {
        "FileName": "goodbye.wasm"
      }
    }
  }

```

Each key-value pair in the `modules` property of the config is responsible for mapping a route (the path part of a URL) to an executable piece of code.

The two required directives for a module section are:

- key: The path-pattern of a URL
- `filename`: The file name of the WebAssembly module to execute

Routes are paths relative to the addresses that the server is listening on. Assuming the routes above are running on a server whose domain is `example.com`:

- The `/` path-pattern handles traffic to `http://example.com/` (or `https://example.com/`)
- A path-pattern like `/hello` would handle traffic to `http://example.com/hello`

The `filename` property is the name of a `wasm` or `wat` file on the filesystem. The filename is combined with the value of `ModulePath` and will be resolved from the current working directory in which the application was started.

### Volume Mounting

In addition to the required directives, the configurations sections support several other properties.
One of these is the `volume` property, which specifies one or more host directories to be mounted as a local directory into the module.

By default, Wasm modules in WAGI have no ability to access the host filesystem.
That is, a Wasm module cannot open `/etc/` and read the files there, even if the application server has access to `/etc/`.
In WAGI, modules are considered untrusted when it comes to accessing resources on the host.
But it is definitely the case that code sometimes needs access to files.

Here is an example of providing a volume:

```json
 "WASM": {
    "ModulePath": "modules",
    "Modules": {
      "/path": {
        "FileName": "bar.wasm",
        "Volumes": {
          "/path/inside/wasm":"/path/on/host"
        }
      }
    }
  }
```

In this case, the `volumes` property tells WAGI to expose the contents of `/path/on/host` to the `bar.wasm` module.
But `bar.wasm` will see that directory as `/path/inside/wasm`. Importantly, it will not be able to access any other parts of the filesystem. For example, it will not see anything on the path `/path/inside`. It _only_ has access to the paths specified
in the `volumes` property.

#### Environment Variables

Similarly to volumes, by default a WebAssembly module cannot access the host's environment variables.
However, the environment property provides a way for you to pass in environment variables:

```json
 "WASM": {
    "ModulePath": "modules",
    "Modules": {
      "/path": {
        "FileName": "hello.wasm",
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
"WASM": {
    "ModulePath": "modules",
    "Modules": {
      "/hello": {
        "FileName": "bar.wasm"
        // With no `entrypoint`, this will invoke `_start()`
      },
      "/entrypoint/hello": {
        "FileName": "bar.wasm",
        "Entrypoint": "hello"
        // Executes the `hello()` function in the module (instead of `_start`)
      },
      "/entrypoint/goodbye": {
        "FileName": "bar.wasm",
        "Entrypoint": "goodbye"
        // Executes the `goodbye()` function in the module (instead of `_start`)
      }
    }
  }
```

## What's Next?

Next, read about [Writing Modules](writing_modules.md) for WAGI.
