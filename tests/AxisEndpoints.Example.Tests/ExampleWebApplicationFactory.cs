using System.Reflection;
using AxisEndpoints.Example.Features.Health;
using AxisEndpoints.Extensions;
using AxisEndpoints.Extensions.CsvHelper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AxisEndpoints.Example.Tests;

public class ExampleWebApplicationFactory : IAsyncLifetime, IDisposable
{
    private WebApplication? _app;
    private HttpClient? _client;
    private static readonly Assembly ExampleAssembly = typeof(HealthEndpoint).Assembly;

    public HttpClient Client =>
        _client ?? throw new InvalidOperationException("Test client is not initialized.");

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddOpenApi();
        builder.Services.AddAxisEndpoints(ExampleAssembly);
        builder.Services.AddAxisEndpointsCsvHelper();

        _app = builder.Build();
        _app.MapOpenApi();
        _app.MapAxisEndpoints(ExampleAssembly);

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _client = null;
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
            _app = null;
        }
    }

    public void Dispose()
    {
        // IAsyncLifetime.DisposeAsync() is called by xUnit; this is a no-op fallback.
    }
}
