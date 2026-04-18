using System.Net;
using AxisEndpoints;

namespace AxisEndpoints.Example.Features.Users.Delete;

/// <summary>
/// Demonstrates:
///   - IEndpoint&lt;TRequest, EmptyResponse&gt; — no response body
///   - 204 No Content via EmptyResponse.Instance
///   - RequireAuthorization(params string[] roles) in a real app:
///       config.Delete("/{id}").RequireAuthorization("Admin")
/// </summary>
public class DeleteUserEndpoint : IEndpoint<DeleteUserRequest, EmptyResponse>
{
    public void Configure(IEndpointConfiguration config)
    {
        config
            .Delete("/{id}")
            .Group<UsersEndpointGroup>()
            .Summary("Delete a user")
            .Description(
                "Permanently removes a user. "
                    + "In a real application, restrict this to the Admin role via .RequireAuthorization(\"Admin\")."
            )
            .AllowAnonymous();
    }

    public Task HandleAsync(
        IResponseSender<EmptyResponse> sender,
        DeleteUserRequest request,
        CancellationToken cancel
    )
    {
        return sender
            .StatusCode(HttpStatusCode.NoContent)
            .SendAsync(EmptyResponse.Instance, cancel);
    }
}
