using System.Net;
using AxisEndpoints;
using AxisEndpoints.Example.Filters;

namespace AxisEndpoints.Example.Features.Users.Create;

/// <summary>
/// Demonstrates:
///   - IEndpoint&lt;TRequest, TResult&gt; with POST body binding
///   - DataAnnotations validation (applied automatically via CreateUserRequest attributes)
///   - Per-endpoint AddFilter&lt;AuditFilter&gt;
///   - 201 Created with Location header
/// </summary>
public class CreateUserEndpoint : IEndpoint<CreateUserRequest, Response<UserResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config
            .Post("/")
            .Group<UsersEndpointGroup>()
            .Summary("Create a user")
            .Description("Creates a new user. Returns 400 if validation fails.")
            .AddFilter<AuditFilter>();
    }

    public Task<Response<UserResponse>> HandleAsync(
        CreateUserRequest request,
        CancellationToken cancel
    )
    {
        // Dummy: assign a fixed ID. A real implementation would persist and return the new ID.
        var created = new UserResponse
        {
            Id = 1,
            Name = request.Name,
            Email = request.Email,
            Role = request.Role,
        };

        return Task.FromResult(
            new Response<UserResponse>
            {
                StatusCode = HttpStatusCode.Created,
                Headers = [("Location", $"/api/users/{created.Id}")],
                Body = created,
            }
        );
    }
}
