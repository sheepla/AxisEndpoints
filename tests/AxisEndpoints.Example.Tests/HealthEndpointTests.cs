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
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("ok");
        body.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));
    }
}
