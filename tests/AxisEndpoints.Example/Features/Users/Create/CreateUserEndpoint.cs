using System.Net;
using AxisEndpoints;
using AxisEndpoints.Example.Filters;

namespace AxisEndpoints.Example.Features.Users.Create;

/// <summary>
/// Demonstrates:
///   - IEndpoint&lt;TRequest, TResponse&gt; with POST body binding
///   - DataAnnotations validation (applied automatically via CreateUserRequest attributes)
///   - Per-endpoint AddFilter&lt;AuditFilter&gt;
///   - 201 Created with Location header
/// </summary>
public class CreateUserEndpoint : IEndpoint<CreateUserRequest, UserResponse>
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

    public Task HandleAsync(
        IResponseSender<UserResponse> sender,
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

        return sender
            .StatusCode(HttpStatusCode.Created)
            .Header("Location", $"/api/users/{created.Id}")
            .SendAsync(created, cancel);
    }
}
