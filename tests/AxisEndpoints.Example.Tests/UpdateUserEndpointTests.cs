using System.Net;
using System.Net.Http.Json;
using AxisEndpoints.Example.Features.Users;
using FluentAssertions;

namespace AxisEndpoints.Example.Tests;

public class UpdateUserEndpointTests : IClassFixture<ExampleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UpdateUserEndpointTests(ExampleWebApplicationFactory factory)
    {
        _client = factory.Client;
    }

    [Fact]
    public async Task UpdateUser_WithFormData_Returns200()
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent("Alice Updated"), "name" },
            { new StringContent("alice-updated@example.com"), "email" },
        };

        var response = await _client.PutAsync("/api/users/1", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(1);
        body.Name.Should().Be("Alice Updated");
        body.Email.Should().Be("alice-updated@example.com");
    }

    [Fact]
    public async Task UpdateUser_WithAvatar_IncludesAvatarInfoInName()
    {
        var avatarContent = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]);
        avatarContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

        var form = new MultipartFormDataContent
        {
            { new StringContent("Bob"), "name" },
            { new StringContent("bob@example.com"), "email" },
            { avatarContent, "avatar", "avatar.png" },
        };

        var response = await _client.PutAsync("/api/users/2", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        body.Should().NotBeNull();
        body!.Name.Should().Contain("avatar.png");
    }
}
