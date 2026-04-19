# AxisEndpoints example application

The `tests/AxisEndpoints.Example` project demonstrates every core feature with minimal dummy logic.

## Running the example

```sh
dotnet run --project tests/AxisEndpoints.Example
```

The API is available at `http://localhost:5081`. The interactive OpenAPI reference is at `http://localhost:5081/scalar/v1`.

## Trying the endpoints

Open `tests/AxisEndpoints.Example/AxisEndpoints.Example.http` in Visual Studio, Rider, or VS Code with the REST Client extension, then send requests one by one.

A quick reference of what each request exercises:

| Request                                       | Feature demonstrated                                      |
| --------------------------------------------- | --------------------------------------------------------- |
| `GET /health`                                 | `IEndpoint<TResponse>`, `AllowAnonymous`                  |
| `GET /api/users`                              | `[FromQuery]` pagination, DataAnnotations on query values |
| `GET /api/users?role=Admin`                   | Optional query filter                                     |
| `GET /api/users/1`                            | `[FromRoute]`, `EndpointContext` header access            |
| `GET /api/users/1` with `Accept-Language: ja` | `EndpointContext` reading request headers                 |
| `GET /api/users/999`                          | Conditional 404 via `RawResponse`                         |
| `POST /api/users` (valid)                     | POST body binding, `AuditFilter`, 201 Created             |
| `POST /api/users` (invalid email)             | DataAnnotations → 400 `ValidationProblemDetails`          |
| `POST /api/users` (name too long)             | `[MaxLength]` → 400 `ValidationProblemDetails`            |
| `PUT /api/users/1`                            | `BindAsync` multipart/form-data                           |
| `DELETE /api/users/1`                         | `EmptyResponse` 204 No Content                            |
| `GET /admin/stats`                            | `[FromQuery]` date range, group-level `LoggingFilter`     |

### Authorization in a real application

The example runs without authentication to keep setup minimal. The comments in each `Configure()` method show the `RequireAuthorization` call you would add in a real application. For example:

```csharp
// Role-based (DeleteUserEndpoint)
config.Delete("/{id}").RequireAuthorization("Admin");

// Named policy (UpdateUserEndpoint) — policy defined in Program.cs via AddAuthorization
config.Put("/{id}").RequireAuthorization("CanManageUsers");

// Dynamic policy (AdminEndpointGroup)
config.RequireAuthorization(policy => policy
    .RequireAuthenticatedUser()
    .RequireRole("Admin"));
```