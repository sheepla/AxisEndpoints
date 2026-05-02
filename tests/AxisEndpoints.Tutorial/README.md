# AxisEndpoints Quick Tutorial

This project is a minimal tutorial for trying out AxisEndpoints with Scalar API reference.

## Requirements

You need .NET SDK 10.0 or later.

## Start From Scratch

```sh
mkdir AxisEndpoints.Tutorial
cd AxisEndpoints.Tutorial
dotnet new webapi
dotnet add package AxisEndpoints
dotnet add package Scalar.AspNetCore
```

## Program.cs

```csharp
using AxisEndpoints.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddAxisEndpoints();

var app = builder.Build();

app.MapOpenApi();
app.MapAxisEndpoints();
app.MapScalarApiReference();

app.Run();
```

## HelloEndpoint.cs

Create `Features/Hello/HelloEndpoint.cs` with the following content:

```csharp
using AxisEndpoints;
using System.Net;

namespace AxisEndpoints.Tutorial.Features.Hello;

public record HelloRequest
{
    public required string Name { get; set; } = string.Empty;
}

public record HelloResponse
{
    public required string Message { get; set; } = string.Empty;
}

public class HelloEndpoint(ILogger<HelloEndpoint> logger) : IEndpoint<HelloRequest, IResult>
{
    public void Configure(IEndpointConfiguration config)
    {
        config
            .Get("/hello")
            .ProducesSuccess<HelloResponse>()
            .ProducesError(HttpStatusCode.BadRequest)
            .Summary("Hello")
            .Description("This endpoint takes a name as input and returns a greeting message.");
    }

    public Task<IResult> HandleAsync(HelloRequest request, CancellationToken cancel)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            logger.LogWarning("Rejected request to /hello because the name was missing.");
            return Task.FromResult(
                Results.Problem(
                    title: "Name is required",
                    detail: "Provide a non-empty name query parameter.",
                    statusCode: StatusCodes.Status400BadRequest
                )
            );
        }

        logger.LogInformation("Received request to /hello with name: {Name}", request.Name);

        return Task.FromResult(
            Results.Json(new HelloResponse
            {
                Message = $"Hello, {request.Name}!",
            })
        );
    }
}
```

## Run It

```sh
dotnet run
```

Open the Scalar API reference at `http://localhost:{port}/scalar`, then try `GET /hello?name=Alice`.

![Scalar API Reference](./assets/scalar.png)
