using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AxisEndpoints.Example.Features.Health;
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

        var schema = document!["paths"]
            ?["/health"]
            ?["get"]
            ?["responses"]
            ?["200"]
            ?["content"]
            ?["application/json"]
            ?["schema"];
        schema.Should().NotBeNull();
        schema!["$ref"]!.GetValue<string>().Should().Be("#/components/schemas/HealthResponse");

        document["components"]?["schemas"]?["ResponseOfHealthResponse"].Should().BeNull();

        var createdSchema =
            document["paths"]
                ?["/api/users"]
                ?["post"]
                ?["responses"]
                ?["201"]
                ?["content"]
                ?["application/json"]
                ?["schema"]
            ?? document["paths"]
                ?["/api/users/"]
                ?["post"]
                ?["responses"]
                ?["201"]
                ?["content"]
                ?["application/json"]
                ?["schema"];
        createdSchema.Should().NotBeNull();
        createdSchema!["$ref"]!.GetValue<string>().Should().Be("#/components/schemas/UserResponse");
    }

    [Fact]
    public async Task OpenApi_RegistersCsvExportSuccessResponseWithTextCsvContentType()
    {
        var document = await _client.GetFromJsonAsync<JsonObject>("/openapi/v1.json");

        document.Should().NotBeNull();

        // CsvResponse<TRow> implements IResult, so this schema only appears because the
        // endpoint declares it explicitly via ProducesSuccess<UserExportRow>(contentType: "text/csv").
        var responses = document!["paths"]?["/api/users/users/export"]?["get"]?["responses"]
            ?.AsObject();
        responses.Should().NotBeNull();

        var successResponse = responses!["200"];
        successResponse.Should().NotBeNull();

        var schema = successResponse!["content"]?["text/csv"]?["schema"];
        schema.Should().NotBeNull();
        schema!["$ref"]!.GetValue<string>().Should().Be("#/components/schemas/UserExportRow");

        // It must not be documented as JSON, since the endpoint never writes application/json.
        successResponse["content"]?["application/json"].Should().BeNull();
    }

    [Fact]
    public async Task OpenApi_DoesNotRegisterEmptyResponseAsJsonBody()
    {
        var document = await _client.GetFromJsonAsync<JsonObject>("/openapi/v1.json");

        document.Should().NotBeNull();

        var responses = document!["paths"]?["/api/users/{id}"]?["delete"]?["responses"]?.AsObject();
        responses.Should().NotBeNull();

        responses!.ContainsKey("200").Should().BeFalse();

        var successResponse = responses["204"];
        successResponse.Should().NotBeNull();
        successResponse!["content"]?["application/json"]?["schema"].Should().BeNull();

        document["components"]?["schemas"]?["ResponseOfEmptyResponse"].Should().BeNull();
        document["components"]?["schemas"]?["EmptyResponse"].Should().BeNull();
    }
}
