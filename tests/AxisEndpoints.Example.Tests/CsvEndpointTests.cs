using System.Net;
using System.Text;
using FluentAssertions;

namespace AxisEndpoints.Example.Tests;

public class CsvEndpointTests : IClassFixture<ExampleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CsvEndpointTests(ExampleWebApplicationFactory factory)
    {
        _client = factory.Client;
    }

    [Fact]
    public async Task ExportCsv_Returns200WithCsvContent()
    {
        var response = await _client.GetAsync("/api/users/users/export");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var csv = await response.Content.ReadAsStringAsync();
        csv.Should().Contain("id");
        csv.Should().Contain("name");
        csv.Should().Contain("Alice");
        csv.Should().Contain("Bob");
    }

    [Fact]
    public async Task ExportCsv_HasContentDispositionHeader()
    {
        var response = await _client.GetAsync("/api/users/users/export");

        response.Content.Headers.ContentDisposition.Should().NotBeNull();
        response.Content.Headers.ContentDisposition.FileName.Should().Be("users.csv");
    }

    [Fact]
    public async Task ImportCsv_ValidCsv_Returns204()
    {
        var csvContent = "name,email,role\nAlice,alice@example.com,Admin\nBob,bob@example.com,User";
        var content = new StringContent(csvContent, Encoding.UTF8, "text/csv");

        var response = await _client.PostAsync("/api/users/users/import", content);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ImportCsv_InvalidRow_Returns400ValidationProblem()
    {
        var csvContent = "name,email,role\n,invalid-email,";
        var content = new StringContent(csvContent, Encoding.UTF8, "text/csv");

        var response = await _client.PostAsync("/api/users/users/import", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
