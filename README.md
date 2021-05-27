# WAGI-dotnet: WebAssembly Gateway Interface for .Net

_WAGI is the easiest way to get started doing cloud-side WebAssembly apps._
_WAGI-dotnet is the easiest way to host WAGI HTTP Handlers in an ASP.Net Core application._

**WARNING:** This is experimental code.
It is not considered production-grade by its developers, neither is it "supported" software.

> DeisLabs is experimenting with many WASM technologies right now.
> This is one of a multitude of projects (including [Krustlet](https://github.com/deislabs/krustlet))
> designed to test the limits of WebAssembly as a cloud-based runtime.

[![Build Status](https://github.com/Deislabs/wagi-dotnet/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Deislabs/wagi-dotnet/actions)

## tl;dr

WAGI allows you to run WebAssembly WASI binaries as HTTP handlers.
WAGI-dotnet provides an extension that enables these handlers to be run in an ASP.Net Core application.
Write a "command line" application that prints a few headers, and compile it to `WASM32-WASI`.
Add an entry to the ASP.Net configuration matching URL to WASM module.
That's it.

You can use any programming language that can compile to `WASM32-WASI`.

## Quickstart

To make it easy to get started with WAGI-dotnet two nuget packages are provided, the first (Deislabs.WAGI.Templates) contains templates that can be used with the dotnet cli tool. The second pcakage contains runtime extension that enable hosting of WAGI modules in ASP.Net using only configuration.  For full details, checkout out the [documentation](docs/README.md).

WAGI-dotnet requires [.Net 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)

To create a ASP.Net Core web application that host a demo WAGI Module:

***Note: nuget.org only contains (pre-)released versions of the packages, to install the latest versions follow the instructions [here](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry) to set up GitHub packages registry as a source for nuget packages.***

``` Console
dotnet new -i Deislabs.WAGI.Templates::0.3.0-preview
```

This will add the dotnet wagi templates. To install pre-release versions of the templates the version of the template package must be provided as a suffix to the package name as show above `::0.3.0-preview`.

``` Console
dotnet new wagi -n hello-wagi
```

This creates a new ASP.Net Core Web application that hosts an example fibonacci WAGI module.

``` Console
cd hello-wagi
dotnet run
```

This starts a ASP.Net Core Web application WAGI host on port 5000 (http) and 5001 (https).

Use a browser or a tool like `curl` to test:

``` Console
$ curl -v http://localhost:5000/fibonacci?93
*   Trying 127.0.0.1...
* TCP_NODELAY set
* Connected to localhost (127.0.0.1) port 5000 (#0)
> GET /fibonacci?93 HTTP/1.1
> Host: localhost:5000
> User-Agent: curl/7.58.0
> Accept: */*
>
< HTTP/1.1 200 OK
< Date: Wed, 05 May 2021 10:49:29 GMT
< Content-Type: text/plain
< Server: Kestrel
< Transfer-Encoding: chunked
<
fib(93)=12200160415121876738
* Connection #0 to host localhost left intact
```

To add your own modules, compile your code to `wasm32-wasi` format and add them to the ASP.Net Configuration (the easiest way to do this is to add entries to appsettings.Development.json).

You can easily create an application that contains your own module by roviding additional options to the ```dotnet new wagi``` command, run ```dotnet new wagi --help``` to see details, alternatively you can edit the application.Development.settings.json configuration file , see [the docs](./docs/coonfigurain_and_running.md) for more details.

### Building and running the application

To build and run the application

``` console

dotnet build -c Release
./bin/Release/net5.0/hello-wagi

```

### Examples and Demos

- [fibonacci](examples/fibonacci/README.md): A simple fibonacci example written in assembly script ([source](https://github.com/simongdavies/fibonacci-wagi-as))
- [watm](examples/watm/README.md): Hello World ! written in [Web Assembly Text Format](https://developer.mozilla.org/en-US/docs/WebAssembly/Understanding_the_text_format)

### Other examples

- [env_wagi](https://github.com/deislabs/env_wagi): Dump the environment that WAGI sets up, including env vars and args.
- [hello-wagi-grain](https://github.com/deislabs/hello-wagi-grain): An easy-to-read Grain example for WAGI.
- [hello-wagi-as](https://github.com/deislabs/hello-wagi-as): AssemblyScript example using environment variables and query params.

If you want to understand the details, read the [Common Gateway Interface 1.1](https://tools.ietf.org/html/rfc3875) specification.
While this is not an exact implementation, it is very close.
See the "Differences" section below for the differences.

## Contributing

We hang out in the [#krustlet](https://kubernetes.slack.com/messages/krustlet) channel of the [Kubernetes Slack](https://kubernetes.slack.com).
If WAGI gains popularity, we'll create a dedicated channel (probably on a more fitting Slack server).

WAGI and WAGI-dotnet are experimental, and we welcome contributions to improve the project.
In fact, we're delighted that you're even reading this section of the docs!

For bug fixes:

- Please file issues for anything you find. This is a new project, and we are SURE there are some bugs to work out.
- If you want to write some code, feel free to open PRs to fix issues. You may want to drop a comment on the issue to let us know you're working on it (so we don't duplicate effort).

For refactors and tests:

- We welcome any changes to improve performance, clean up code, add tests, etc.

For features:

- If there is already an issue for that feature, please let us know in the comments if you plan on working on it.
- If you have a new idea, file an issue describing it, and we will happily discuss it.

Since this is an experimental repository, we might be a little slow to answer.

## Code of Conduct

This project has adopted the [Microsoft Open Source Code of
Conduct](https://opensource.microsoft.com/codeofconduct/).

For more information see the [Code of Conduct
FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact
[opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
