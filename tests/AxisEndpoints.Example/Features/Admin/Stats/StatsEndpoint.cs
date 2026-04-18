using AxisEndpoints;
using AxisEndpoints.Example.Features.Admin;

namespace AxisEndpoints.Example.Features.Admin.Stats;

public class StatsResponse
{
    public required int TotalUsers { get; init; }
    public required int NewUsersInPeriod { get; init; }
    public required DateTimeOffset From { get; init; }
    public required DateTimeOffset To { get; init; }
}

/// <summary>
/// Demonstrates:
///   - IEndpoint&lt;TRequest, TResponse&gt; with [FromQuery] binding (GET)
///   - Group-level RequireAuthorization(builder) via AdminEndpointGroup
/// </summary>
public class StatsEndpoint : IEndpoint<StatsRequest, StatsResponse>
{
    public void Configure(IEndpointConfiguration config)
    {
        config
            .Get("/stats")
            .Group<AdminEndpointGroup>()
            .Summary("Get user statistics")
            .Description("Returns aggregated user counts for the specified time window.");
    }

    public Task HandleAsync(
        IResponseSender<StatsResponse> sender,
        StatsRequest request,
        CancellationToken cancel
    )
    {
        var to = request.To ?? DateTimeOffset.UtcNow;
        var from = request.From ?? to.AddDays(-30);

        return sender.SendAsync(
            new StatsResponse
            {
                TotalUsers = 42,
                NewUsersInPeriod = 7,
                From = from,
                To = to,
            },
            cancel
        );
    }
}
