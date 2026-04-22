using AxisEndpoints;

namespace AxisEndpoints.Example.Features.Users.Delete;

/// <summary>
/// Demonstrates:
///   - IEndpoint&lt;TRequest, TResult&gt; with no response body
///   - Response.NoContent shorthand
///   - RequireAuthorization(params string[] roles) in a real app:
///       config.Delete("/{id}").RequireAuthorization("Admin")
/// </summary>
public class DeleteUserEndpoint : IEndpoint<DeleteUserRequest, Response<EmptyResponse>>
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

    public Task<Response<EmptyResponse>> HandleAsync(
        DeleteUserRequest request,
        CancellationToken cancel
    )
    {
        return Task.FromResult(Response.NoContent);
    }
}
