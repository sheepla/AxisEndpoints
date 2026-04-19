using System.Net;

namespace AxisEndpoints.Example.Features.Users.FindById;

/// <summary>
/// Demonstrates:
///   - IEndpoint&lt;TRequest, TResponse&gt; with [FromRoute] binding (GET)
///   - EndpointContext: reading the Accept-Language request header
///   - Conditional 404 via sender.StatusCode(HttpStatusCode.NotFound)
/// </summary>
public class FindUserByIdEndpoint : IEndpoint<FindUserByIdRequest, UserResponse>
{
    private readonly EndpointContext _context;

    public FindUserByIdEndpoint(EndpointContext context)
    {
        _context = context;
    }

    public void Configure(IEndpointConfiguration config)
    {
        config
            .Get("/{id}")
            .Group<UsersEndpointGroup>()
            .Summary("Find a user by ID")
            .Description(
                "Returns a single user. Reads Accept-Language to demonstrate EndpointContext header access."
            );
    }

    public Task HandleAsync(
        IResponseSender<UserResponse> sender,
        FindUserByIdRequest request,
        CancellationToken cancel
    )
    {
        // Demonstrate EndpointContext: read Accept-Language from request headers.
        var language = _context.RequestHeaders["Accept-Language"].FirstOrDefault() ?? "en";

        // Dummy: only ID 1 exists. Any other ID returns 404.
        if (request.Id != 1)
        {
            return sender
                .StatusCode(HttpStatusCode.NotFound)
                .SendAsync(
                    new UserResponse
                    {
                        Id = 0,
                        Name = string.Empty,
                        Email = string.Empty,
                        Role = string.Empty,
                    },
                    cancel
                );
        }

        var user = new UserResponse
        {
            Id = 1,
            Name = language.StartsWith("ja") ? "山田 太郎" : "Alice",
            Email = "alice@example.com",
            Role = "User",
        };

        return sender.SendAsync(user, cancel);
    }
}
