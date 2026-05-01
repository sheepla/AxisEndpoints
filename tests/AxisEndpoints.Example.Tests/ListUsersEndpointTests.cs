using System.Net;
using System.Net.Http.Json;
using AxisEndpoints.Example.Features.Users;
using AxisEndpoints.Example.Features.Users.List;
using FluentAssertions;

namespace AxisEndpoints.Example.Tests;

public class ListUsersEndpointTests : IClassFixture<ExampleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ListUsersEndpointTests(ExampleWebApplicationFactory factory)
    {
        _client = factory.Client;
    }

    [Fact]
    public async Task ListUsers_DefaultPagination_ReturnsAllUsers()
    {
        var response = await _client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListUsersResponse>();
        body.Should().NotBeNull();
        body!.TotalCount.Should().Be(4);
        body.Items.Should().HaveCount(4);
        body.Page.Should().Be(1);
        body.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task ListUsers_WithExplicitPagination_ReturnsPagedResults()
    {
        var response = await _client.GetAsync("/api/users?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListUsersResponse>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCount(2);
        body.TotalCount.Should().Be(4);
        body.Page.Should().Be(1);
        body.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task ListUsers_WithRoleFilter_ReturnsFilteredResults()
    {
        var response = await _client.GetAsync("/api/users?role=Admin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListUsersResponse>();
        body.Should().NotBeNull();
        body!.TotalCount.Should().Be(1);
        body.Items.Should().ContainSingle();
        body.Items[0].Name.Should().Be("Alice");
        body.Items[0].Role.Should().Be("Admin");
    }

    [Fact]
    public async Task ListUsers_SecondPage_ReturnsCorrectItems()
    {
        var response = await _client.GetAsync("/api/users?page=2&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListUsersResponse>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCount(2);
        body.Items[0].Name.Should().Be("Charlie");
        body.Items[1].Name.Should().Be("Diana");
    }

    [Fact]
    public async Task ListUsers_InvalidPage_Returns400()
    {
        var response = await _client.GetAsync("/api/users?page=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListUsers_InvalidPageSize_Returns400()
    {
        var response = await _client.GetAsync("/api/users?pageSize=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
