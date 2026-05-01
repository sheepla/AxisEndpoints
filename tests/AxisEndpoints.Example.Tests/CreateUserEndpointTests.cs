using System.Net;
using System.Net.Http.Json;
using AxisEndpoints.Example.Features.Users;
using FluentAssertions;

namespace AxisEndpoints.Example.Tests;

public class CreateUserEndpointTests : IClassFixture<ExampleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CreateUserEndpointTests(ExampleWebApplicationFactory factory)
    {
        _client = factory.Client;
    }

    [Fact]
    public async Task CreateUser_ValidRequest_Returns201WithLocationHeader()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            Name = "Eve",
            Email = "eve@example.com",
            Role = "User",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/users/1");

        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Eve");
        body.Email.Should().Be("eve@example.com");
        body.Role.Should().Be("User");
    }

    [Fact]
    public async Task CreateUser_DefaultRole_ReturnsUser()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            Name = "Frank",
            Email = "frank@example.com",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        body.Should().NotBeNull();
        body!.Role.Should().Be("User");
    }

    [Fact]
    public async Task CreateUser_InvalidEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            Name = "Eve",
            Email = "not-an-email",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateUser_NameTooLong_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            Name = new string('A', 101),
            Email = "toolong@example.com",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateUser_MissingName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            Email = "missing@example.com",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
