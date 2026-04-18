# AxisEndpoints

*This project is currently under development. Specifications are subject to change without notice.*

## About

**AxisEndpoints** is a DSL for implementing the Request-Endpoint-Response a.k.a. REPR pattern. It provides **Axis** which consolidates the endpoints of your application!

- **Clear and explicit programming interface**: the REPR pattern can be implemented in a simple and robust way.
- **Well-suited for Vertical Slice Architecture projects**: The REPR pattern is ideal for Vertical Slice Architecture, which involves slicing applications into functional units. Since each API endpoint operates as a self-contained unit, this approach ensures scalability and loose coupling between slices.
- **OpenAPI First**: Since type annotation for requests and responses is enforced and you can add summaries and descriptions for each endpoint, your OpenAPI documentation becomes more detailed. This makes it easier to integrate with frontend applications written in TypeScript and other API clients, as well as to collaborate with other developers. Have you ever forgotten to include `TypedResults` on Minimal API or `ActionResult<T>` in Controllers? That won't happen anymore.
- **Modular package structure**: Since extensions are provided as separate packages, you can include only the features you truly need in your application.
- **Low learning curve**: It provides a safe interface while maintaining the level of abstraction of Minimal APIs.


## Why?

There are three ways to implement a Web API in ASP.NET Core:
Minimal API, Controller, and the REPR pattern. The REPR pattern is the third option, following Minimal API and Controller.
The REPR pattern is a way to implement an API using three components: HTTP requests, responses, and endpoints, which serve as the entry points to the application.

- **Minimal API**: It is simple and highly flexible. It also offers excellent performance and provides a programming interface that is familiar to developers coming from Python, TypeScript, and other languages.
However, if modules are not properly separated, numerous endpoints end up in the same place, making it difficult to scale reliably.
- **Controller**: This pattern allows you to group multiple API endpoints and manage CRUD operations collectively, making it widely used by C# developers familiar with ASP.NET Core MVC. However, there is a concern that as functionality increases, the controllers themselves may become bloated.

The REPR pattern helps build robust and scalable APIs by enforcing the structure of requests, responses, and endpoints through type and interface contracts, thereby preventing interference with other endpoints. It is considered to be highly compatible with the Vertical Slice Architecture pattern,
enabling the creation of loosely coupled structures that are designed to evolve into modular monoliths or microservices.


However, to implement the REPR pattern, I had to either wrap the Minimal API myself or rely on a library like FastEndpoints.
That said, FastEndpoints is built on its own design philosophy, and while it is feature-rich, I found it to have a steep learning curve.
As a result, I felt I needed a library that offered a minimal, explicit programming interface that respected the standard ASP.NET Core API, which led me to build one myself.

## Installation

### Install from local nupkg

```sh
# Create the nuget package
dotnet pack src/AxisEndpoints/AxisEndpoints.csproj -o <LocalNupkgDirectory>

# Install AxisEndpoints from local nupkg
dotnet add <YourProject> package AxisEndpoints --source <LocalNupkgDirectory>
```

### Install from nuget.org

*Planning*

## How to use

### Setup on Program.cs

Call `AddAxisEndpoints()` on the service collection and `MapAxisEndpoints()` on the application. Both methods scan the entry assembly automatically. Pass an `Assembly` argument to target a specific project.

```csharp
using AxisEndpoints.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddAxisEndpoints(); // Discovers and registers all endpoints via DI

var app = builder.Build();

app.MapOpenApi();
app.MapAxisEndpoints(); // Maps all discovered endpoints to the Minimal API pipeline

app.Run();
```

### Defining endpoints with IEndpoint

#### `IEndpoint<TRequest, TResponse>` — request body + response

Use for POST, PUT, and PATCH endpoints. `TRequest` is bound from the JSON request body.

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

public class CreateUserEndpoint : IEndpoint<CreateUserRequest, CreateUserResponse>
{
    private readonly IUserRepository _repository;

    public CreateUserEndpoint(IUserRepository repository) => _repository = repository;

    public void Configure(IEndpointConfiguration config)
    {
        config.Post("/users")
            .Tags("Users")
            .Summary("Create a new user");
    }

    public async Task HandleAsync(
        IResponseSender<CreateUserResponse> sender,
        CreateUserRequest request,
        CancellationToken cancel)
    {
        var id = await _repository.CreateAsync(request.Name, request.Email, cancel);
        await sender.StatusCode(HttpStatusCode.Created).SendAsync(new CreateUserResponse { Id = id }, cancel);
    }
}
```

#### `IEndpoint<TRequest, TResponse>` — route / query parameters + response

For GET and DELETE, `TRequest` is bound from route values and query string instead of the body. Annotate properties with `[FromRoute]` or `[FromQuery]` to clarify the source.

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

public class GetUserEndpoint : IEndpoint<GetUserRequest, GetUserResponse>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Get("/users/{id}")
            .Tags("Users")
            .Summary("Get a user by ID");
    }

    public async Task HandleAsync(
        IResponseSender<GetUserResponse> sender,
        GetUserRequest request,
        CancellationToken cancel)
    {
        // fetch user ...
        await sender.SendAsync(new GetUserResponse { Id = request.Id, Name = "Alice" }, cancel);
    }
}
```

#### `IEndpoint<TResponse>` — no parameters

Use only when the endpoint truly takes no parameters at all. If you need query parameters, define a request type with `[FromQuery]` and use `IEndpoint<TRequest, TResponse>` instead.

```csharp
public class HealthResponse { public required string Status { get; init; } }

public class HealthEndpoint : IEndpoint<HealthResponse>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Get("/health").AllowAnonymous();
    }

    public Task HandleAsync(IResponseSender<HealthResponse> sender, CancellationToken cancel)
    {
        return sender.StatusCode(HttpStatusCode.OK).SendAsync(new HealthResponse { Status = "ok" }, cancel);
    }
}
```

#### `IEndpoint<TRequest, EmptyResponse>` — no response body

Use for endpoints that return no body (e.g. 204 No Content). Send `EmptyResponse.Instance` and the response body is skipped automatically.

```csharp
public class DeleteUserRequest
{
    [FromRoute]
    public required int Id { get; init; }
}

public class DeleteUserEndpoint : IEndpoint<DeleteUserRequest, EmptyResponse>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Delete("/users/{id}").Summary("Delete a user");
    }

    public async Task HandleAsync(
        IResponseSender<EmptyResponse> sender,
        DeleteUserRequest request,
        CancellationToken cancel)
    {
        // delete user ...
        await sender.StatusCode(HttpStatusCode.NoContent).SendAsync(EmptyResponse.Instance, cancel);
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

A failed request returns a response like the following:

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

For logic-heavy validation that DataAnnotations cannot express, FluentValidation can be integrated via a custom `IEndpointFilter`.
AxisEndpoints does not provide a dedicated FluentValidation package because the filter approach is straightforward and keeps the dependency opt-in.

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

public class CreateUserEndpoint : IEndpoint<CreateUserRequest, CreateUserResponse>
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
public class AdminEndpoint : IEndpoint<AdminRequest, AdminResponse>
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
public class ManageUsersEndpoint : IEndpoint<ManageUsersRequest, ManageUsersResponse>
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
public class ReportsEndpoint : IEndpoint<ReportsRequest, ReportsResponse>
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
public class HealthEndpoint : IEndpoint<HealthResponse>
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

### Accessing HTTP resources

For accessing request headers, the authenticated user, or writing a raw response stream (e.g. binary downloads, CSV), inject `EndpointContext` via the constructor. Do not inject it unless needed — typed request/response covers most cases.

```csharp
public class DownloadReportEndpoint : IEndpoint<DownloadReportRequest, EmptyResponse>
{
    private readonly EndpointContext _context;
    private readonly IReportService _reports;

    public DownloadReportEndpoint(EndpointContext context, IReportService reports)
    {
        _context = context;
        _reports = reports;
    }

    public void Configure(IEndpointConfiguration config)
    {
        config.Get("/reports/{id}/download").Summary("Download a report as CSV");
    }

    public async Task HandleAsync(
        IResponseSender<EmptyResponse> sender,
        DownloadReportRequest request,
        CancellationToken cancel)
    {
        var lang = _context.RequestHeaders["Accept-Language"].FirstOrDefault() ?? "en";
        _context.RawResponse.ContentType = "text/csv";
        _context.RawResponse.Headers["Content-Disposition"] = "attachment; filename=report.csv";
        await _reports.WriteCsvAsync(_context.RawResponse.Body, request.Id, lang, cancel);
    }
}
```

For file uploads, implement the Minimal API `BindAsync` convention on the request type. The library routes to it automatically.

```csharp
public class UploadRequest
{
    public required IFormFile File { get; init; }

    public static async ValueTask<UploadRequest> BindAsync(HttpContext context, ParameterInfo _)
    {
        var form = await context.Request.ReadFormAsync();
        var file = form.Files["file"] ?? throw new BadHttpRequestException("'file' field is required.");
        return new UploadRequest { File = file };
    }
}
```

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

public class GetUsersEndpoint : IEndpoint<GetUsersResponse>
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

Implement `IEndpointFilter` and register it with `config.AddFilter<TFilter>()`. Filters are resolved from DI per request, so constructor injection is supported. Register a filter on the group to apply it to all endpoints in that group.

```csharp
public class LoggingFilter : IEndpointFilter
{
    private readonly ILogger<LoggingFilter> _logger;

    public LoggingFilter(ILogger<LoggingFilter> logger) => _logger = logger;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        _logger.LogInformation("{Method} {Path}", context.HttpContext.Request.Method, context.HttpContext.Request.Path);
        var result = await next(context);
        _logger.LogInformation("Response: {StatusCode}", context.HttpContext.Response.StatusCode);
        return result;
    }
}

// Per-endpoint filter
public class MyEndpoint : IEndpoint<MyRequest, MyResponse>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Post("/items").AddFilter<LoggingFilter>();
    }
    // ...
}

// Group-level filter (applied to all endpoints in the group)
public class MyGroup : IEndpointGroup
{
    public void Configure(IEndpointGroupConfiguration config)
    {
        config.Prefix("/api/items").AddFilter<LoggingFilter>();
    }
}
```

You can also implement custom filters that apply only to specific endpoints. This mechanism is similar to ASP.NET Core middleware, but whereas middleware applies to all requests, `IEndpointFilter` acts more like a hook that can be applied only to specific endpoints.

## License

MIT
