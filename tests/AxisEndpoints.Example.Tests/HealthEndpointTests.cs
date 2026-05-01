using System.Net;
using System.Net.Http.Json;
using AxisEndpoints.Example.Features.Health;
using FluentAssertions;

namespace AxisEndpoints.Example.Tests;

public class HealthEndpointTests : IClassFixture<ExampleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(ExampleWebApplicationFactory factory)
    {
        _client = factory.Client;
    }

    [Fact]
    public async Task GetHealth_Returns200WithStatus()
    {
        var before = DateTimeOffset.UtcNow;
        var response = await _client.GetAsync("/health");
        var after = DateTimeOffset.UtcNow;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("ok");
        body.Timestamp.Should().BeOnOrAfter(before.AddSeconds(-1));
        body.Timestamp.Should().BeOnOrBefore(after.AddSeconds(1));
    }
}
