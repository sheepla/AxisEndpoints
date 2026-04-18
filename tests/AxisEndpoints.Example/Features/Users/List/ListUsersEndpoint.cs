using AxisEndpoints;

namespace AxisEndpoints.Example.Features.Users.List;

public class ListUsersResponse
{
    public required IReadOnlyList<UserResponse> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
}

/// <summary>
/// Demonstrates:
///   - IEndpoint&lt;TRequest, TResponse&gt; with multiple [FromQuery] parameters (GET)
///   - DataAnnotations on query-bound values (Page, PageSize range validation)
///   - Paginated response shape
/// </summary>
public class ListUsersEndpoint : IEndpoint<ListUsersRequest, ListUsersResponse>
{
    public void Configure(IEndpointConfiguration config)
    {
        config
            .Get("/")
            .Group<UsersEndpointGroup>()
            .Summary("List users")
            .Description("Returns a paginated list of users. Supports optional role filtering.");
    }

    public Task HandleAsync(
        IResponseSender<ListUsersResponse> sender,
        ListUsersRequest request,
        CancellationToken cancel
    )
    {
        // Dummy data — a real implementation would query a repository.
        var allUsers = new List<UserResponse>
        {
            new()
            {
                Id = 1,
                Name = "Alice",
                Email = "alice@example.com",
                Role = "Admin",
            },
            new()
            {
                Id = 2,
                Name = "Bob",
                Email = "bob@example.com",
                Role = "User",
            },
            new()
            {
                Id = 3,
                Name = "Charlie",
                Email = "charlie@example.com",
                Role = "User",
            },
            new()
            {
                Id = 4,
                Name = "Diana",
                Email = "diana@example.com",
                Role = "Manager",
            },
        };

        var filtered = request.Role is not null
            ? allUsers.Where(u => u.Role == request.Role).ToList()
            : allUsers;

        var items = filtered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return sender.SendAsync(
            new ListUsersResponse
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = filtered.Count,
            },
            cancel
        );
    }
}
