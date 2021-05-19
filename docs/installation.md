# Installing the WAGI extension

The WAGI extension is installed using nuget.

## Prerequisites

- [.Net 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)

## Create a new ASP.Net application

To create a new, run the following command:

```console
$ dotnet new web
  The template "ASP.NET Core Empty" was created successfully.

Processing post-creation actions...
Running 'dotnet restore' on /tmp/wagitest/wagitest.csproj...
  Determining projects to restore...
  Restored /tmp/wagitest/wagitest.csproj (in 100 ms).
Restore succeeded.
```

## Install the WAGI extension package

``` console
dotnet add package Deislabs.WAGI  --prerelease
 Determining projects to restore...
  Writing /tmp/tmpYrGzPC.tmp
info : Adding PackageReference for package 'Deislabs.WAGI' into project '/tmp/wagiproj/WagiTest.csproj'.
info : Restoring packages for /tmp/wagiproj/WagiTest.csproj...
info : Package 'Deislabs.WAGI' is compatible with all the specified frameworks in project '/tmp/wagiproj/WagiTest.csproj'.
info : PackageReference for package 'Deislabs.WAGI' version '0.2.0-preview' updated in file '/tmp/wagiproj/WagiTest.csproj'.
info : Committing restore...
info : Writing assets file to disk. Path: /tmp/wagiproj/obj/project.assets.json
log  : Restored /tmp/wagiproj/WagiTest.csproj (in 155 ms).
```

## Install the WAGI extension package from Github packages

Only released versions of the WAGI extension are availble from nuget.org, more recent builds are available in Github Packages, to install a nuget package from Github:

```console
dotnet add package Deislabs.WAGI --prerelease -s https://nuget.pkg.github.com/deislabs/index.json
```

## Add Wagi endpoint configuration

Modify `Startup.cs` to configure endpoints for WAGI modules.

In method `Configure` modify the call to method `app.UseEndpoints` from this:

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
      endpoints.MapWASMModules();
  });
```

## What's Next?

Continue on to [Configuring and Running WAGI Modules](configuring_and_running.md) to learn about configuring the application.
