using System.Net;
using System.Net.Http.Json;
using AxisEndpoints.Example.Features.Users;
using FluentAssertions;

namespace AxisEndpoints.Example.Tests;

public class FindByIdEndpointTests : IClassFixture<ExampleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FindByIdEndpointTests(ExampleWebApplicationFactory factory)
    {
        _client = factory.Client;
    }

    [Fact]
    public async Task FindById_ExistingUser_Returns200()
    {
        var response = await _client.GetAsync("/api/users/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(1);
        body.Name.Should().Be("Alice");
        body.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task FindById_NonExistingUser_Returns404()
    {
        var response = await _client.GetAsync("/api/users/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FindById_WithJapaneseLanguageHeader_ReturnsJapaneseName()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/1");
        request.Headers.Add("Accept-Language", "ja");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        body.Should().NotBeNull();
        body!.Name.Should().Contain("山田");
    }
}
