# AxisEndpoints

*This project is currently under development. Specifications are subject to change without notice.*

## Table of Contents

- [AxisEndpoints](#axisendpoints)
  - [Table of Contents](#table-of-contents)
  - [About](#about)
  - [Features](#features)
  - [Packages](#packages)
  - [Installation](#installation)
    - [Install from local nupkg](#install-from-local-nupkg)
    - [Install from nuget.org](#install-from-nugetorg)
  - [Why? — Choosing between Minimal API, Controllers, and the REPR Pattern](#why--choosing-between-minimal-api-controllers-and-the-repr-pattern)
  - [Usage](#usage)
    - [Setup in Program.cs](#setup-in-programcs)
    - [Response type quick reference](#response-type-quick-reference)
    - [Defining endpoints with IEndpoint](#defining-endpoints-with-iendpoint)
      - [JSON response — `Response<TBody>`](#json-response--responsetbody)
      - [No response body — `Response<EmptyResponse>`](#no-response-body--responseemptyresponse)
      - [No request parameters — `IEndpoint<TResult>`](#no-request-parameters--iendpointtresult)
      - [Route and query parameters](#route-and-query-parameters)
    - [Validation with DataAnnotations](#validation-with-dataannotations)
    - [Validation with FluentValidation](#validation-with-fluentvalidation)
    - [Authorization](#authorization)
      - [Require roles](#require-roles)
      - [Require a named policy](#require-a-named-policy)
      - [Require a dynamically constructed policy](#require-a-dynamically-constructed-policy)
      - [Allow anonymous access](#allow-anonymous-access)
      - [Group-level authorization](#group-level-authorization)
    - [Accessing HTTP context](#accessing-http-context)
    - [Grouping multiple endpoints](#grouping-multiple-endpoints)
    - [Adding custom filters](#adding-custom-filters)
  - [CSV extension — AxisEndpoints.Extensions.CsvHelper](#csv-extension--axisendpointsextensionscsvhelper)
    - [Setup](#setup)
    - [CSV import — `CsvRequest<TRow>`](#csv-import--csvrequesttrow)
    - [CSV export — `CsvResponse<TRow>`](#csv-export--csvresponsetrow)
    - [Per-row validation](#per-row-validation)
    - [Custom column mapping with ClassMap](#custom-column-mapping-with-classmap)
  - [Author](#author)
  - [License](#license)


## About

**AxisEndpoints** is a DSL for implementing the Request-Endpoint-Response (REPR) pattern in ASP.NET Core. It consolidates each API endpoint into a self-contained class with a clear, explicit programming interface.

- **Clear and explicit programming interface**: each endpoint declares its request type, result type, route, and metadata in one place.
- **Modular package structure**: extensions are provided as separate packages so you can include only the features you need.
- **Gentle learning curve**: AxisEndpoints is a lightweight wrapper around the Minimal API. Developers familiar with Minimal API should find it easy to adopt.
- **Well-suited for Vertical Slice Architecture**: the REPR pattern is a natural fit for [Vertical Slice Architecture](https://learn.microsoft.com/en-us/shows/on-dotnet/on-dotnet-live-clean-architecture-vertical-slices-and-modular-monoliths-oh-my), where each feature is a self-contained unit with loose coupling between slices.

## Features

- **OpenAPI-first and type-safe**: request and response types are enforced by the interface, and per-endpoint summaries and descriptions are supported. OpenAPI output is generated automatically without requiring `TypedResults` or `ActionResult<T>` annotations.
- **Automatic validation with DataAnnotations**: built-in attribute-based validation using `System.ComponentModel.DataAnnotations` runs automatically before `HandleAsync` is called.
- **Endpoint grouping**: apply a shared route prefix, tags, authorization policy, and filters to multiple endpoints at once.
- **Custom filter support**: attach `IEndpointFilter` implementations to individual endpoints or groups, independently of the global middleware pipeline.
- **Type-safe CSV input and output** *(optional)*: the `AxisEndpoints.Extensions.CsvHelper` extension package adds typed CSV import and export without pulling CsvHelper into the core.

## Packages

| Package                              | Description                                                                                                                                                    |
| ------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AxisEndpoints`                      | Core package. Provides `IEndpoint<TRequest, TResult>` and related primitives.                                                                                  |
| `AxisEndpoints.Extensions.CsvHelper` | Optional. Integrates [CsvHelper](https://joshclose.github.io/CsvHelper/) for typed CSV import (`CsvRequest<TRow>`) and streaming export (`CsvResponse<TRow>`). |

## Installation

### Install from local nupkg

```sh
# Build the NuGet package
dotnet pack src/AxisEndpoints/AxisEndpoints.csproj -o <LocalNupkgDirectory>

# Add it to your project
dotnet add <YourProject> package AxisEndpoints --source <LocalNupkgDirectory>
```

### Install from nuget.org

*Planning*


## Why? — Choosing between Minimal API, Controllers, and the REPR Pattern

ASP.NET Core offers three approaches to building Web APIs. Each involves different trade-offs:

|                     | Minimal API                                   | Controller                                  | REPR Pattern (AxisEndpoints)                   |
| ------------------- | --------------------------------------------- | ------------------------------------------- | ---------------------------------------------- |
| **Structure**       | Functions registered inline                   | Methods grouped in a class                  | One class per endpoint                         |
| **Scalability**     | ⚠ Can become hard to manage as endpoints grow | ⚠ Controllers can grow bloated over time    | ✅ Each endpoint stays self-contained           |
| **Coupling**        | Low — but no enforced structure               | Medium — CRUD operations share a controller | Low — slices are independent by design         |
| **Learning curve**  | Low                                           | Medium (MVC conventions)                    | Medium (built on Minimal API)                  |
| **Best suited for** | Small services, prototypes                    | CRUD-heavy APIs familiar to MVC developers  | Feature-rich APIs, Vertical Slice Architecture |

The REPR pattern is a good choice when:

- Your API has many endpoints that grow independently of each other.
- You are applying Vertical Slice Architecture and want each feature to be self-contained.
- You want the compiler to enforce that every endpoint declares its request and response types.
- You want OpenAPI documentation to stay accurate without manual annotations.

To implement the REPR pattern in ASP.NET Core, you can either write a thin wrapper around the Minimal API yourself, or use a library such as [FastEndpoints](https://fast-endpoints.com/). FastEndpoints is powerful, but its abstractions diverge noticeably from standard ASP.NET Core conventions, which steepens the learning curve. AxisEndpoints takes a different approach: it stays close to the Minimal API surface, so the concepts you already know continue to apply.

## Usage

### Setup in Program.cs

Call `AddAxisEndpoints()` on the service collection and `MapAxisEndpoints()` on the application. Both methods scan the entry assembly automatically. Pass an `Assembly` argument to target a specific project.

```csharp
using AxisEndpoints.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddAxisEndpoints(); // Discovers and registers all endpoints

var app = builder.Build();

app.MapOpenApi();
app.MapAxisEndpoints(); // Maps all discovered endpoints to the Minimal API pipeline

app.Run();
```

### Response type quick reference

`HandleAsync` returns `Task<TResult>`. The value of `TResult` determines how the response is written:

| Scenario                         | Return type                  | Example                                                                                  |
| -------------------------------- | ---------------------------- | ---------------------------------------------------------------------------------------- |
| JSON response                    | `Response<TBody>`            | `return new Response<UserResponse> { Body = user }`                                      |
| JSON with status code or headers | `Response<TBody>`            | `return new Response<UserResponse> { StatusCode = HttpStatusCode.Created, Body = user }` |
| No response body                 | `Response<EmptyResponse>`    | `return Response.NoContent`                                                              |
| CSV export or other stream       | `IResult` implementation     | `return Task.FromResult(CsvResponse.From(rows))`                                         |
| Fully custom response            | Any `IResult` implementation | `return Task.FromResult(new MyCustomResult(...))`                                        |

`Response<TBody>` has three properties. Only `Body` is required:

```csharp
new Response<UserResponse>
{
    StatusCode = HttpStatusCode.Created,          // defaults to 200 OK
    Headers    = [("Location", $"/users/{id}")],  // defaults to empty
    Body       = new UserResponse { Id = id },    // required
}
```

The static shorthands `Response.Empty` (200 OK, no body) and `Response.NoContent` (204 No Content) cover the most common no-body cases.

### Defining endpoints with IEndpoint

#### JSON response — `Response<TBody>`

Use `IEndpoint<TRequest, Response<TBody>>` for POST, PUT, and PATCH endpoints where `TRequest` is bound from the JSON request body.

```csharp
public class CreateUserRequest
{
    public required string Name { get; init; }
    public required string Email { get; init; }
}

public class CreateUserResponse
{
    public required int Id { get; init; }
}

public class CreateUserEndpoint : IEndpoint<CreateUserRequest, Response<CreateUserResponse>>
{
    private readonly IUserRepository _repository;

    public CreateUserEndpoint(IUserRepository repository) => _repository = repository;

    public void Configure(IEndpointConfiguration config)
    {
        config.Post("/users")
            .Tags("Users")
            .Summary("Create a new user");
    }

    public async Task<Response<CreateUserResponse>> HandleAsync(
        CreateUserRequest request,
        CancellationToken cancel)
    {
        var id = await _repository.CreateAsync(request.Name, request.Email, cancel);

        return new Response<CreateUserResponse>
        {
            StatusCode = HttpStatusCode.Created,
            Headers    = [("Location", $"/users/{id}")],
            Body       = new CreateUserResponse { Id = id },
        };
    }
}
```

#### No response body — `Response<EmptyResponse>`

Use `Response<EmptyResponse>` for endpoints that return no body. The `Response.NoContent` shorthand returns 204 No Content in a single expression.

```csharp
public class DeleteUserEndpoint : IEndpoint<DeleteUserRequest, Response<EmptyResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Delete("/users/{id}").Summary("Delete a user");
    }

    public Task<Response<EmptyResponse>> HandleAsync(
        DeleteUserRequest request,
        CancellationToken cancel)
    {
        // delete user ...
        return Task.FromResult(Response.NoContent);
    }
}
```

`Response.Empty` is the 200 OK equivalent for the same no-body pattern.

#### No request parameters — `IEndpoint<TResult>`

Use `IEndpoint<TResult>` when the endpoint takes no parameters at all. If query parameters are needed, define a request type with `[FromQuery]` properties and use `IEndpoint<TRequest, TResult>` instead.

```csharp
public class HealthEndpoint : IEndpoint<Response<HealthResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Get("/health").AllowAnonymous();
    }

    public Task<Response<HealthResponse>> HandleAsync(CancellationToken cancel)
    {
        return Task.FromResult(new Response<HealthResponse>
        {
            Body = new HealthResponse { Status = "ok", Timestamp = DateTimeOffset.UtcNow },
        });
    }
}
```

#### Route and query parameters

For GET and DELETE, `TRequest` is bound from route values and the query string rather than the request body. Annotate properties with `[FromRoute]` or `[FromQuery]` to clarify the binding source.

```csharp
public class GetUserRequest
{
    [FromRoute]
    public required int Id { get; init; }
}

public class GetUserResponse
{
    public required int Id { get; init; }
    public required string Name { get; init; }
}

public class GetUserEndpoint : IEndpoint<GetUserRequest, Response<GetUserResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Get("/users/{id}")
            .Tags("Users")
            .Summary("Get a user by ID");
    }

    public async Task<Response<GetUserResponse>> HandleAsync(
        GetUserRequest request,
        CancellationToken cancel)
    {
        var user = await _repository.FindByIdAsync(request.Id, cancel);

        if (user is null)
        {
            return new Response<GetUserResponse>
            {
                StatusCode = HttpStatusCode.NotFound,
                Body       = new GetUserResponse { Id = 0, Name = string.Empty },
            };
        }

        return new Response<GetUserResponse>
        {
            Body = new GetUserResponse { Id = user.Id, Name = user.Name },
        };
    }
}
```

For custom binding (e.g. multipart/form-data), implement the Minimal API `BindAsync` convention on the request type. AxisEndpoints detects it automatically:

```csharp
public class UploadRequest
{
    public required IFormFile File { get; init; }

    public static async ValueTask<UploadRequest> BindAsync(HttpContext context)
    {
        var form = await context.Request.ReadFormAsync();
        var file = form.Files["file"] ?? throw new BadHttpRequestException("'file' field is required.");
        return new UploadRequest { File = file };
    }
}
```

### Validation with DataAnnotations

DataAnnotations attributes on `TRequest` are validated automatically before `HandleAsync` is called.
On failure, a `400 Bad Request` response is returned in the standard [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457) problem details format.

```csharp
public class CreateUserRequest
{
    [Required]
    [MaxLength(100)]
    public required string Name { get; init; }

    [Required]
    [EmailAddress]
    public required string Email { get; init; }
}
```

A failed request returns a response in the following shape:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["The Email field is not a valid e-mail address."]
  }
}
```

To opt out of automatic validation globally, pass options to `AddAxisEndpoints`:

```csharp
builder.Services.AddAxisEndpoints(options =>
{
    options.DisableDataAnnotationsValidation = true;
});
```

### Validation with FluentValidation

For validation logic that DataAnnotations cannot express, FluentValidation can be integrated via a custom `IEndpointFilter`. AxisEndpoints does not provide a dedicated FluentValidation package because the filter approach is straightforward and keeps the dependency opt-in.

```csharp
// A reusable filter that resolves IValidator<TRequest> from DI.
public class FluentValidationFilter<TRequest> : IEndpointFilter
{
    private readonly IValidator<TRequest> _validator;

    public FluentValidationFilter(IValidator<TRequest> validator) => _validator = validator;

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();

        if (request is not null)
        {
            var result = await _validator.ValidateAsync(request);

            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                return TypedResults.ValidationProblem(errors);
            }
        }

        return await next(context);
    }
}

// Register the validator and apply the filter to the endpoint.
builder.Services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();

public class CreateUserEndpoint : IEndpoint<CreateUserRequest, Response<CreateUserResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Post("/users")
            .Tags("Users")
            .AddFilter<FluentValidationFilter<CreateUserRequest>>();
    }
    // ...
}
```

### Authorization

#### Require roles

Pass one or more role names to restrict access to users in at least one of those roles.

```csharp
public class AdminEndpoint : IEndpoint<AdminRequest, Response<AdminResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Get("/admin/stats").RequireAuthorization("Admin", "SuperAdmin");
    }
    // ...
}
```

#### Require a named policy

Reference a policy defined in `AddAuthorization` by name.

```csharp
// Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanManageUsers", policy =>
        policy.RequireRole("Admin").RequireClaim("department", "engineering"));
});

// Endpoint
public class ManageUsersEndpoint : IEndpoint<ManageUsersRequest, Response<ManageUsersResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Post("/users/manage").RequireAuthorization("CanManageUsers");
    }
    // ...
}
```

#### Require a dynamically constructed policy

Build a policy inline using `AuthorizationPolicyBuilder` when a named policy is not needed.

```csharp
public class ReportsEndpoint : IEndpoint<ReportsRequest, Response<ReportsResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Get("/reports")
            .RequireAuthorization(policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim("subscription", "pro", "enterprise"));
    }
    // ...
}
```

#### Allow anonymous access

```csharp
public class HealthEndpoint : IEndpoint<Response<HealthResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Get("/health").AllowAnonymous();
    }
    // ...
}
```

#### Group-level authorization

Authorization set on a group applies to all endpoints in that group. Per-endpoint settings take precedence.

```csharp
public class UsersGroup : IEndpointGroup
{
    public void Configure(IEndpointGroupConfiguration config)
    {
        config.Prefix("/api/users")
            .Tags("Users")
            .RequireAuthorization("CanManageUsers");
    }
}
```

### Accessing HTTP context

For reading request headers, accessing the authenticated user, or inspecting route values beyond what `TRequest` covers, inject `EndpointContext` via the constructor. Avoid injecting it when typed request/response is sufficient.

```csharp
public class FindByIdEndpoint : IEndpoint<FindByIdRequest, Response<UserResponse>>
{
    private readonly EndpointContext _context;

    public FindByIdEndpoint(EndpointContext context) => _context = context;

    public void Configure(IEndpointConfiguration config)
    {
        config.Get("/users/{id}").Summary("Get a user by ID");
    }

    public Task<Response<UserResponse>> HandleAsync(
        FindByIdRequest request,
        CancellationToken cancel)
    {
        var language = _context.RequestHeaders["Accept-Language"].FirstOrDefault() ?? "en";
        // ...
    }
}
```

`EndpointContext` exposes the following members:

| Member               | Type                | Description                                              |
| -------------------- | ------------------- | -------------------------------------------------------- |
| `RequestHeaders`     | `IHeaderDictionary` | Incoming request headers                                 |
| `User`               | `ClaimsPrincipal`   | The authenticated user                                   |
| `Query`              | `IQueryCollection`  | Raw query string values                                  |
| `GetRouteValue(key)` | `string?`           | A single route parameter by name                         |
| `RawResponse`        | `HttpResponse`      | Escape hatch for writing directly to the response stream |

`RawResponse` is intended for cases that `Response<TBody>` and extension packages cannot cover. For CSV output, the `AxisEndpoints.Extensions.CsvHelper` package is the preferred approach.

### Grouping multiple endpoints

Implement `IEndpointGroup` and reference it with `config.Group<TGroup>()`. All endpoints in the group share the prefix, tags, authorization policy, and filters defined on the group.

```csharp
public class UsersGroup : IEndpointGroup
{
    public void Configure(IEndpointGroupConfiguration config)
    {
        config.Prefix("/api/users")
            .Tags("Users")
            .RequireAuthorization();
    }
}

public class GetUsersEndpoint : IEndpoint<Response<GetUsersResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        // Resolves to GET /api/users
        config.Get("/").Group<UsersGroup>().Summary("Get all users");
    }
    // ...
}
```

### Adding custom filters

Implement `IEndpointFilter` and register it with `config.AddFilter<TFilter>()`. Filters are resolved from DI per request, so constructor injection is supported. Registering a filter on a group applies it to all endpoints in that group.

```csharp
public class LoggingFilter : IEndpointFilter
{
    private readonly ILogger<LoggingFilter> _logger;

    public LoggingFilter(ILogger<LoggingFilter> logger) => _logger = logger;

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        _logger.LogInformation("{Method} {Path}",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path);

        var result = await next(context);

        _logger.LogInformation("Response: {StatusCode}",
            context.HttpContext.Response.StatusCode);

        return result;
    }
}

// Per-endpoint
public class MyEndpoint : IEndpoint<MyRequest, Response<MyResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Post("/items").AddFilter<LoggingFilter>();
    }
    // ...
}

// Group-level (applied to all endpoints in the group)
public class MyGroup : IEndpointGroup
{
    public void Configure(IEndpointGroupConfiguration config)
    {
        config.Prefix("/api/items").AddFilter<LoggingFilter>();
    }
}
```

`IEndpointFilter` differs from middleware in that it can be scoped to individual endpoints or groups, rather than applying globally to all requests.

## CSV extension — AxisEndpoints.Extensions.CsvHelper

The `AxisEndpoints.Extensions.CsvHelper` package adds typed CSV import and export to AxisEndpoints endpoints, backed by [CsvHelper](https://joshclose.github.io/CsvHelper/).

### Setup

Install the extension package alongside the core:

```sh
dotnet add <YourProject> package AxisEndpoints.Extensions.CsvHelper --source <LocalNupkgDirectory>
```

Register the extension services in `Program.cs`:

```csharp
using AxisEndpoints.Extensions;
using AxisEndpoints.Extensions.CsvHelper;

builder.Services.AddAxisEndpoints();
builder.Services.AddAxisEndpointsCsvHelper(); // registers CsvBindingExceptionFilter
```

### CSV import — `CsvRequest<TRow>`

Derive your request type from `CsvRequest<TRow>`. After binding, `request.Rows` contains the parsed rows as `IReadOnlyList<TRow>`.

Minimal API requires `BindAsync` to be declared as a non-generic static method on the concrete type, so it cannot be provided by the base class. Declare it in the derived class and delegate to `BindCsvAsync<T>`:

```csharp
public sealed class UserImportRow
{
    [Name("name")]  public string Name  { get; init; } = string.Empty;
    [Name("email")] public string Email { get; init; } = string.Empty;
    [Name("role")]  public string Role  { get; init; } = string.Empty;
}

public sealed class ImportUsersRequest : CsvRequest<UserImportRow>
{
    public static ValueTask<ImportUsersRequest> BindAsync(HttpContext context)
        => BindCsvAsync<ImportUsersRequest>(context);
}

public sealed class ImportUsersEndpoint : IEndpoint<ImportUsersRequest, Response<EmptyResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Post("/users/import")
            .AddFilter<CsvBindingExceptionFilter>()
            .Summary("Import users from CSV");
    }

    public Task<Response<EmptyResponse>> HandleAsync(
        ImportUsersRequest request,
        CancellationToken cancel)
    {
        foreach (var row in request.Rows) { /* persist row */ }
        return Task.FromResult(Response.NoContent);
    }
}
```

Both `text/csv` direct body and `multipart/form-data` file uploads are supported automatically.

### CSV export — `CsvResponse<TRow>`

Return `CsvResponse<TRow>` directly from `HandleAsync`. It implements `IResult`, so the framework streams the rows to the response without buffering the entire dataset in memory.

```csharp
public sealed class UserExportRow
{
    [Name("id")]    public int    Id    { get; init; }
    [Name("name")]  public string Name  { get; init; } = string.Empty;
    [Name("email")] public string Email { get; init; } = string.Empty;
}

public sealed class ExportUsersEndpoint : IEndpoint<CsvResponse<UserExportRow>>
{
    private readonly IUserRepository _repository;

    public ExportUsersEndpoint(IUserRepository repository) => _repository = repository;

    public void Configure(IEndpointConfiguration config)
    {
        config.Get("/users/export").Summary("Export users as CSV");
    }

    public Task<CsvResponse<UserExportRow>> HandleAsync(CancellationToken cancel)
    {
        // IAsyncEnumerable<T> is written row-by-row without loading everything into memory.
        var rows = _repository.GetAllAsync(cancel);
        return Task.FromResult(CsvResponse.From(rows, fileName: "users.csv"));
    }
}
```

`CsvResponse.From` also accepts `IEnumerable<T>` for synchronous sequences.

### Per-row validation

DataAnnotations attributes on `TRow` are validated during binding, one row at a time. All errors are collected before the handler is invoked and surfaced as an RFC 9457 `ValidationProblem` response by `CsvBindingExceptionFilter`.

Error keys follow the pattern `"row {n}: {MemberName}"`:

```json
{
  "status": 400,
  "errors": {
    "row 3: Email": ["The Email field is not a valid e-mail address."],
    "row 5: Name":  ["The Name field is required."]
  }
}
```

Register `CsvBindingExceptionFilter` on any endpoint that accepts a `CsvRequest<TRow>` parameter:

```csharp
config.Post("/users/import").AddFilter<CsvBindingExceptionFilter>();
```

### Custom column mapping with ClassMap

Override `CreateClassMap()` on the derived request type to supply a CsvHelper `ClassMap` instead of relying on attributes:

```csharp
public sealed class ImportUsersRequest : CsvRequest<UserImportRow>
{
    public static ValueTask<ImportUsersRequest> BindAsync(HttpContext context)
        => BindCsvAsync<ImportUsersRequest>(context);

    protected override ClassMap CreateClassMap() => new UserImportRowMap();
}

public sealed class UserImportRowMap : ClassMap<UserImportRow>
{
    public UserImportRowMap()
    {
        Map(r => r.Name).Name("full_name");
        Map(r => r.Email).Name("email_address");
    }
}
```

The same `CreateClassMap()` override is available on `CsvResponse<TRow>` via the `classMap` parameter of `CsvResponse.From`:

```csharp
CsvResponse.From(rows, classMap: new UserExportRowMap(), fileName: "users.csv")
```

## Author

[sheepla](https://github.com/sheepla)

## License

See [LICENSE](./LICENSE).