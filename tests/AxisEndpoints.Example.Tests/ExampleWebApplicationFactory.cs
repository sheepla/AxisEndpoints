using System.Reflection;
using AxisEndpoints.Example.Features.Health;
using AxisEndpoints.Extensions;
using AxisEndpoints.Extensions.CsvHelper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace AxisEndpoints.Example.Tests;

public class ExampleWebApplicationFactory : IAsyncLifetime, IDisposable
{
    private WebApplication? _app;
    private static readonly Assembly ExampleAssembly = typeof(HealthEndpoint).Assembly;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddAxisEndpoints(ExampleAssembly);
        builder.Services.AddAxisEndpointsCsvHelper();

        _app = builder.Build();
        _app.MapAxisEndpoints(ExampleAssembly);

        await _app.StartAsync();
        Client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}
