using System.Net;
using FluentAssertions;

namespace AxisEndpoints.Example.Tests;

public class DeleteUserEndpointTests : IClassFixture<ExampleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DeleteUserEndpointTests(ExampleWebApplicationFactory factory)
    {
        _client = factory.Client;
    }

    [Fact]
    public async Task DeleteUser_Returns204NoContent()
    {
        var response = await _client.DeleteAsync("/api/users/1");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
