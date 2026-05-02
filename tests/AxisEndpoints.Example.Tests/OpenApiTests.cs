using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;

namespace AxisEndpoints.Example.Tests;

public class OpenApiTests : IClassFixture<ExampleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OpenApiTests(ExampleWebApplicationFactory factory)
    {
        _client = factory.Client;
    }

    [Fact]
    public async Task OpenApi_UsesBodyTypeForJsonResponseEndpoints()
    {
        var document = await _client.GetFromJsonAsync<JsonObject>("/openapi/v1.json");

        document.Should().NotBeNull();

        var schema = document!["paths"]?["/health"]?["get"]?["responses"]?["200"]?["content"]?["application/json"]?["schema"];
        schema.Should().NotBeNull();
        schema!["$ref"]!.GetValue<string>().Should().Be("#/components/schemas/HealthResponse");

        document["components"]?["schemas"]?["ResponseOfHealthResponse"].Should().BeNull();
    }

    [Fact]
    public async Task OpenApi_DoesNotRegisterEmptyResponseAsJsonBody()
    {
        var document = await _client.GetFromJsonAsync<JsonObject>("/openapi/v1.json");

        document.Should().NotBeNull();

        var responses = document!["paths"]?["/api/users/{id}"]?["delete"]?["responses"]?.AsObject();
        responses.Should().NotBeNull();

        var successResponse = responses!["200"] ?? responses["204"];
        successResponse.Should().NotBeNull();
        successResponse!["content"]?["application/json"]?["schema"].Should().BeNull();

        document["components"]?["schemas"]?["ResponseOfEmptyResponse"].Should().BeNull();
        document["components"]?["schemas"]?["EmptyResponse"].Should().BeNull();
    }
}
