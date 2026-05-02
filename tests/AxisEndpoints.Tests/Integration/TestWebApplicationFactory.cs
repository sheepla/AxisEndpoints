using System.Reflection;
using AxisEndpoints.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AxisEndpoints.Tests.Integration;

public class TestWebApplicationFactory : IAsyncLifetime, IDisposable
{
    private WebApplication? _app;
    private readonly Assembly _testAssembly = typeof(HelloEndpoint).Assembly;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddOpenApi();
        builder.Services.AddAxisEndpoints(_testAssembly);

        _app = builder.Build();
        _app.MapOpenApi();
        _app.MapAxisEndpoints(_testAssembly);

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
