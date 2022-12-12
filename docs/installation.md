# Installing the WAGI extension

The WAGI extension is installed using nuget. There are 2 nuget packages, one contains wagi-dotnet and the other provides a set of templates to create new wagi-dotnet projects.

## Prerequisites

- [.Net 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)

## Install the WAGI extension package

``` console
dotnet add package Deislabs.WAGI  --prerelease
 Determining projects to restore...
  Writing /tmp/tmpYrGzPC.tmp
info : Adding PackageReference for package 'Deislabs.WAGI' into project '/tmp/wagiproj/WagiTest.csproj'.
info : Restoring packages for /tmp/wagiproj/WagiTest.csproj...
info : Package 'Deislabs.WAGI' is compatible with all the specified frameworks in project '/tmp/wagiproj/WagiTest.csproj'.
info : PackageReference for package 'Deislabs.WAGI' version '0.12.0-preview' updated in file '/tmp/wagiproj/WagiTest.csproj'.
info : Committing restore...
info : Writing assets file to disk. Path: /tmp/wagiproj/obj/project.assets.json
log  : Restored /tmp/wagiproj/WagiTest.csproj (in 155 ms).
```

## Install the WAGI extension package from Github packages

Only released versions of the WAGI extension are availble from nuget.org, more recent builds are available in Github Packages, to install a nuget package from Github:

```console
dotnet add package Deislabs.WAGI --prerelease -s https://nuget.pkg.github.com/deislabs/index.json
```
## Use the wagi-dotnet templates

### Create a project using a locally available WAGI module

```
$ dotnet new wagi 
The template "ASP.NET WAGI Web App" was created successfully.
```

This will create an applciation configured to use a sample WAGI module. Run `dotnet new wagi --help` to see the available options for the template.

### Create a project using a WAGI Module stored in a [bindle server](https://github.com/deislabs/bindle).

```
$ dotnet new wagi-bindle --bindle-server-url <url of the bindle server> --bindle-name <name of the bindle>
The template "ASP.NET WAGI Bindle Web App" was created successfully.
```

## Use an ASP.Net application without using the wagi-dotnet templates.

To create a new application, run the following command:

```console
$ dotnet new wagi 
The template "ASP.NET WAGI Web App" was created successfully.

Processing post-creation actions...
Running 'dotnet restore' on /tmp/wagitest/wagitest.csproj...
  Determining projects to restore...
  Restored /tmp/wagitest/wagitest.csproj (in 100 ms).
Restore succeeded.
```

### Modify `Startup.cs` to configure endpoints for WAGI modules.

Add a constructor and property to your `Startup.cs` file:

``` csharp
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }
```

Add the following code to the ConfigureServices method:

``` csharp
    services.AddHttpClient();
    services.AddWagi(Configuration);
```

The HttpClient service is required to allow Wagi modules to make outgoing HTTP requests using  [wasi-experimental-http](https://github.com/deislabs/wasi-experimental-http). (see [here](configuring_and-running.md#making-http-requests-from-modules) for details.).

In the `Configure` method modify the call to method `app.UseEndpoints` from this:

``` csharp
  app.UseEndpoints(endpoints =>
  {
      endpoints.MapGet("/", async context =>
      {
          await context.Response.WriteAsync("Hello World!");
      });
  });
```

to this:

``` csharp
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapWagiModules();
    });
```

Calling `MapWagiModules()` maps all configured wagi modules.

## What's Next?

Continue on to [Configuring and Running WAGI Modules](configuring_and_running.md) to learn about configuring the application.
