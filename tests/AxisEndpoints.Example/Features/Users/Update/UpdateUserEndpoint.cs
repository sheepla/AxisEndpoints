using AxisEndpoints;

namespace AxisEndpoints.Example.Features.Users.Update;

/// <summary>
/// Demonstrates:
///   - IEndpoint&lt;TRequest, TResult&gt; with custom BindAsync (multipart/form-data)
///   - RequireAuthorization(string policyName) in a real app:
///       config.Put("/{id}").RequireAuthorization("CanManageUsers")
///     where "CanManageUsers" is defined in Program.cs via AddAuthorization.
/// </summary>
public class UpdateUserEndpoint : IEndpoint<UpdateUserRequest, Response<UserResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config
            .Put("/{id}")
            .Group<UsersEndpointGroup>()
            .Summary("Update a user")
            .Description(
                "Updates name, email, and optional avatar. "
                    + "In a real application, restrict via .RequireAuthorization(\"CanManageUsers\")."
            )
            .AllowAnonymous();
    }

    public Task<Response<UserResponse>> HandleAsync(
        UpdateUserRequest request,
        CancellationToken cancel
    )
    {
        var avatarNote = request.Avatar is not null
            ? $" (avatar: {request.Avatar.FileName}, {request.Avatar.Length} bytes)"
            : string.Empty;

        return Task.FromResult(
            new Response<UserResponse>
            {
                Body = new UserResponse
                {
                    Id = request.Id,
                    Name = request.Name + avatarNote,
                    Email = request.Email,
                    Role = "User",
                },
            }
        );
    }
}
