

<div align="right">

[![NuGet](https://img.shields.io/nuget/v/AxisEndpoints?label=AxisEndpoints)](https://www.nuget.org/packages/AxisEndpoints)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AxisEndpoints)](https://www.nuget.org/packages/AxisEndpoints)

</div>

<div align="right">

[![NuGet](https://img.shields.io/nuget/v/AxisEndpoints.Extensions.CsvHelper?label=AxisEndpoints.Extensions.CsvHelper)](https://www.nuget.org/packages/AxisEndpoints.Extensions.CsvHelper)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AxisEndpoints.Extensions.CsvHelper)](https://www.nuget.org/packages/AxisEndpoints.Extensions.CsvHelper)

</div>

<div align="center">

# AxisEndpoints

</div>


<div align="center">

![CI Status](https://github.com/sheepla/AxisEndpoints/actions/workflows/ci.yml/badge.svg) ![Publish Status](https://github.com/sheepla/AxisEndpoints/actions/workflows/publish.yml/badge.svg)
![Docs Status](https://github.com/sheepla/AxisEndpoints/actions/workflows/docs.yml/badge.svg)

</div>

## About

**AxisEndpoints** is a library for implementing the Request-Endpoint-Response (REPR) pattern in ASP.NET Core. It consolidates each API endpoint into a self-contained class with a clear, explicit programming interface.

- **Clear and explicit programming interface**: each endpoint declares its request type, result type, route, and metadata in one place.
- **Modular package structure**: extensions are provided as separate packages so you can include only the features you need.
- **Gentle learning curve**: AxisEndpoints is a lightweight wrapper around the Minimal API. Developers familiar with Minimal API should find it easy to adopt.
- **Well-suited for Vertical Slice Architecture**: the REPR pattern is a natural fit for Vertical Slice Architecture, where each feature is a self-contained unit with loose coupling between slices.

## Installation

### Install from nuget.org (Recommended)

Packages are available on nuget.org:

| Package                              | Description                                                                                                                                                    |
| ------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [AxisEndpoints](https://www.nuget.org/packages/AxisEndpoints)                      | Core package. Provides `IEndpoint<TRequest, TResult>` and related primitives.                                                                                  |
| [AxisEndpoints.Extensions.CsvHelper](https://www.nuget.org/packages/AxisEndpoints.Extensions.CsvHelper) | Optional. Integrates CsvHelper for typed CSV import (`CsvRequest<TRow>`) and streaming export (`CsvResponse<TRow>`). |

You can install with .NET CLI with the following command:

```sh
dotnet add package AxisEndpoints
```

Additionally, if you want to use the CSV import/export features, install the CsvHelper extension package:

```sh
dotnet add package AxisEndpoints.Extensions.CsvHelper
```

### Install from local nupkg

Alternatively, you can build the NuGet package from source and install it locally. This is useful if you want to use the latest features that haven't been published to nuget.org yet, or if you want to contribute to the project.

```sh
# Build the NuGet package
dotnet pack src/AxisEndpoints/AxisEndpoints.csproj -o path/to/YourLocalNupkgDirectory

# Add it to your project
dotnet add path/to/YourProject.csproj package AxisEndpoints --source path/to/YourLocalNupkgDirectory
```

## Documentation

For detailed usage guides, API reference, and examples, visit the [documentation site](https://sheepla.github.io/AxisEndpoints/).

## Author

[sheepla](https://github.com/sheepla)

## License

See [LICENSE](./LICENSE).
