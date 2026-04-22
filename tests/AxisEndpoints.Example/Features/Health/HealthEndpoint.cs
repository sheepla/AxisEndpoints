namespace AxisEndpoints.Example.Features.Health;

public class HealthResponse
{
    public required string Status { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Demonstrates:
///   - IEndpoint&lt;TResult&gt; (no request parameters)
///   - AllowAnonymous
/// </summary>
public class HealthEndpoint : IEndpoint<Response<HealthResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config
            .Get("/health")
            .Tags("Health")
            .Summary("Health check")
            .Description(
                "Returns the current service status. Always accessible without authentication."
            )
            .AllowAnonymous();
    }

    public Task<Response<HealthResponse>> HandleAsync(CancellationToken cancel)
    {
        return Task.FromResult(
            new Response<HealthResponse>
            {
                Body = new HealthResponse { Status = "ok", Timestamp = DateTimeOffset.UtcNow },
            }
        );
    }
}
