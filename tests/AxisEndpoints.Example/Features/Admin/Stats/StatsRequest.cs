using Microsoft.AspNetCore.Mvc;

namespace AxisEndpoints.Example.Features.Admin.Stats;

/// <summary>
/// Demonstrates [FromQuery] binding for a GET endpoint.
/// </summary>
public class StatsRequest
{
    /// <summary>Start of the aggregation window (inclusive). Defaults to 30 days ago.</summary>
    [FromQuery]
    public DateTimeOffset? From { get; init; }

    /// <summary>End of the aggregation window (inclusive). Defaults to now.</summary>
    [FromQuery]
    public DateTimeOffset? To { get; init; }
}
