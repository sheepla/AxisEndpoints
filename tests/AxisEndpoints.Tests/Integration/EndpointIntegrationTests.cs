using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;

namespace AxisEndpoints.Tests.Integration;

public class EndpointIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public EndpointIntegrationTests(TestWebApplicationFactory factory)
    {
        _client = factory.Client;
    }

    [Fact]
    public async Task GetHello_Returns200WithMessage()
    {
        var response = await _client.GetAsync("/hello");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HelloResponse>();
        body.Should().NotBeNull();
        body!.Message.Should().Be("Hello, World!");
    }

    [Fact]
    public async Task PostEcho_Returns200WithEchoedValue()
    {
        var response = await _client.PostAsJsonAsync("/echo", new { Message = "test" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EchoResponse>();
        body.Should().NotBeNull();
        body!.Echo.Should().Be("test");
    }

    [Fact]
    public async Task PostValidated_WithInvalidBody_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/validated", new { Name = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostValidated_WithValidBody_Returns200()
    {
        var response = await _client.PostAsJsonAsync("/validated", new { Name = "Valid" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteItem_Returns204()
    {
        var response = await _client.DeleteAsync("/items/42");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetGrouped_Returns200WithGroupPrefix()
    {
        var response = await _client.GetAsync("/api/grouped");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HelloResponse>();
        body.Should().NotBeNull();
        body!.Message.Should().Be("Grouped!");
    }

    [Fact]
    public async Task OpenApi_UsesResponseBodyTypeInsteadOfResponseWrapper()
    {
        var document = await _client.GetFromJsonAsync<JsonObject>("/openapi/v1.json");

        document.Should().NotBeNull();

        var schema = document!["paths"]?["/hello"]?["get"]?["responses"]?["200"]?["content"]?["application/json"]?["schema"];
        schema.Should().NotBeNull();
        schema!["$ref"]!.GetValue<string>().Should().Be("#/components/schemas/HelloResponse");

        document["components"]?["schemas"]?["ResponseOfHelloResponse"].Should().BeNull();
    }

    [Fact]
    public async Task OpenApi_DoesNotRegisterEmptyResponseAsJsonBody()
    {
        var document = await _client.GetFromJsonAsync<JsonObject>("/openapi/v1.json");

        document.Should().NotBeNull();

        var responses = document!["paths"]?["/items/{id}"]?["delete"]?["responses"]?.AsObject();
        responses.Should().NotBeNull();

        var successResponse = responses!["200"] ?? responses["204"];
        successResponse.Should().NotBeNull();
        successResponse!["content"]?["application/json"]?["schema"].Should().BeNull();

        document["components"]?["schemas"]?["ResponseOfEmptyResponse"].Should().BeNull();
        document["components"]?["schemas"]?["EmptyResponse"].Should().BeNull();
    }
}
