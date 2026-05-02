

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

## About

**AxisEndpoints** is a DSL for implementing the Request-Endpoint-Response (REPR) pattern in ASP.NET Core. It consolidates each API endpoint into a self-contained class with a clear, explicit programming interface.

- **Clear and explicit programming interface**: each endpoint declares its request type, result type, route, and metadata in one place.
- **Modular package structure**: extensions are provided as separate packages so you can include only the features you need.
- **Gentle learning curve**: AxisEndpoints is a lightweight wrapper around the Minimal API. Developers familiar with Minimal API should find it easy to adopt.
- **Well-suited for Vertical Slice Architecture**: the REPR pattern is a natural fit for Vertical Slice Architecture, where each feature is a self-contained unit with loose coupling between slices.

## Packages

Packages are available on nuget.org.

| Package                              | Description                                                                                                                                                    |
| ------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [AxisEndpoints](https://www.nuget.org/packages/AxisEndpoints)                      | Core package. Provides `IEndpoint<TRequest, TResult>` and related primitives.                                                                                  |
| [AxisEndpoints.Extensions.CsvHelper](https://www.nuget.org/packages/AxisEndpoints.Extensions.CsvHelpe) | Optional. Integrates CsvHelper for typed CSV import (`CsvRequest<TRow>`) and streaming export (`CsvResponse<TRow>`). |

## Installation

### Install from nuget.org (Recommended)

```sh
dotnet add package AxisEndpoints
```

For the CSV extension:

```sh
dotnet add package AxisEndpoints.Extensions.CsvHelper
```

### Install from local nupkg

```sh
# Build the NuGet package
dotnet pack src/AxisEndpoints/AxisEndpoints.csproj -o <LocalNupkgDirectory>

# Add it to your project
dotnet add <YourProject> package AxisEndpoints --source <LocalNupkgDirectory>
```

## Documentation

For detailed usage guides, API reference, and examples, visit the [documentation site](https://sheepla.github.io/AxisEndpoints/).

## Author

[sheepla](https://github.com/sheepla)

## License

See [LICENSE](./LICENSE).
