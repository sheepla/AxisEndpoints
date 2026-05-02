using System.Net;

namespace AxisEndpoints.Example.Features.Users.FindById;

/// <summary>
/// Demonstrates:
///   - IEndpoint&lt;TRequest, IResult&gt; for endpoints that return multiple response shapes
///   - ProducesSuccess / ProducesError for explicit OpenAPI schema declaration
///   - EndpointContext: reading the Accept-Language request header
///   - 404 ProblemDetails response when the resource is not found
/// </summary>
public class FindByIdEndpoint : IEndpoint<FindByIdRequest, IResult>
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
            )
            .ProducesSuccess<UserResponse>()
            .ProducesError(HttpStatusCode.NotFound);
    }

    public Task<IResult> HandleAsync(FindByIdRequest request, CancellationToken cancel)
    {
        // Demonstrate EndpointContext: read Accept-Language from request headers.
        var language = _context.RequestHeaders["Accept-Language"].FirstOrDefault() ?? "en";

        // Dummy: only ID 1 exists. Any other ID returns 404.
        if (request.Id != 1)
        {
            return Task.FromResult(
                Results.Problem(
                    statusCode: (int)HttpStatusCode.NotFound,
                    title: "User not found",
                    detail: $"No user with ID {request.Id} exists."
                )
            );
        }

        return Task.FromResult(
            Results.Json(
                new UserResponse
                {
                    Id = 1,
                    Name = language.StartsWith("ja") ? "山田 太郎" : "Alice",
                    Email = "alice@example.com",
                    Role = "User",
                }
            )
        );
    }
}
