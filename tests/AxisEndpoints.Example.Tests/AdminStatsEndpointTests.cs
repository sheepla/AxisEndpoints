using System.Net;
using System.Net.Http.Json;
using AxisEndpoints.Example.Features.Admin.Stats;
using FluentAssertions;

namespace AxisEndpoints.Example.Tests;

public class AdminStatsEndpointTests : IClassFixture<ExampleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AdminStatsEndpointTests(ExampleWebApplicationFactory factory)
    {
        _client = factory.Client;
    }

    [Fact]
    public async Task GetStats_DefaultDateRange_Returns200()
    {
        var response = await _client.GetAsync("/admin/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StatsResponse>();
        body.Should().NotBeNull();
        body!.TotalUsers.Should().Be(42);
        body.NewUsersInPeriod.Should().Be(7);
    }

    [Fact]
    public async Task GetStats_WithDateRange_Returns200WithCorrectDates()
    {
        var response = await _client.GetAsync(
            "/admin/stats?from=2025-01-01T00:00:00Z&to=2025-12-31T00:00:00Z"
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StatsResponse>();
        body.Should().NotBeNull();
        body!.From.Should().Be(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
        body.To.Should().Be(new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero));
    }
}
