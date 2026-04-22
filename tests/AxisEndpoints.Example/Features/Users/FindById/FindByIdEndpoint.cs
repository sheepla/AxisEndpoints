using System.Net;
using AxisEndpoints;

namespace AxisEndpoints.Example.Features.Users.FindById;

/// <summary>
/// Demonstrates:
///   - IEndpoint&lt;TRequest, TResult&gt; with [FromRoute] binding (GET)
///   - EndpointContext: reading the Accept-Language request header
///   - Conditional 404 response
/// </summary>
public class FindByIdEndpoint : IEndpoint<FindByIdRequest, Response<UserResponse>>
{
    private readonly EndpointContext _context;

    public FindByIdEndpoint(EndpointContext context)
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

    public Task<Response<UserResponse>> HandleAsync(
        FindByIdRequest request,
        CancellationToken cancel
    )
    {
        // Demonstrate EndpointContext: read Accept-Language from request headers.
        var language = _context.RequestHeaders["Accept-Language"].FirstOrDefault() ?? "en";

        // Dummy: only ID 1 exists. Any other ID returns 404.
        if (request.Id != 1)
        {
            return Task.FromResult(
                new Response<UserResponse>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Body = new UserResponse
                    {
                        Id = 0,
                        Name = string.Empty,
                        Email = string.Empty,
                        Role = string.Empty,
                    },
                }
            );
        }

        return Task.FromResult(
            new Response<UserResponse>
            {
                Body = new UserResponse
                {
                    Id = 1,
                    Name = language.StartsWith("ja") ? "山田 太郎" : "Alice",
                    Email = "alice@example.com",
                    Role = "User",
                },
            }
        );
    }
}
