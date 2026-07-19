using System.Net;
using System.Reflection;
using System.Text.Encodings.Web;
using AxisEndpoints.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AxisEndpoints.Tests.Integration;

public class AuthorizationIntegrationTests : IClassFixture<AuthorizationWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthorizationIntegrationTests(AuthorizationWebApplicationFactory factory)
    {
        _client = factory.Client;
    }

    [Fact]
    public async Task Endpoint_RequireAuthorizationWithoutRoles_ReturnsUnauthorizedWhenUnauthenticated()
    {
        var response = await _client.GetAsync("/protected");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Group_RequireAuthorizationWithoutRoles_ReturnsUnauthorizedWhenUnauthenticated()
    {
        var response = await _client.GetAsync("/secured/resource");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Endpoint_AllowAnonymous_ReturnsOk()
    {
        var response = await _client.GetAsync("/public");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Endpoint_AllowAnonymous_OverridesGroupRequireAuthorization()
    {
        // The group requires authorization, but this specific endpoint opts out of it.
        // The endpoint-level setting must take precedence over the group's.
        var response = await _client.GetAsync("/override/anonymous");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Endpoint_WithoutOwnAuthorizationSetting_InheritsGroupRequireAuthorization()
    {
        // This endpoint declares no authorization setting of its own, so it should fall
        // back to the group's RequireAuthorization() and return 401 when unauthenticated.
        var response = await _client.GetAsync("/override/inherited");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public class ProtectedEndpoint : IEndpoint<Response<HelloResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Get("/protected").RequireAuthorization();
    }

    public Task<Response<HelloResponse>> HandleAsync(CancellationToken cancel) =>
        Task.FromResult(new Response<HelloResponse> { Body = new HelloResponse("secret") });
}

public class PublicEndpoint : IEndpoint<Response<HelloResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Get("/public").AllowAnonymous();
    }

    public Task<Response<HelloResponse>> HandleAsync(CancellationToken cancel) =>
        Task.FromResult(new Response<HelloResponse> { Body = new HelloResponse("public") });
}

public class SecuredGroup : IEndpointGroup
{
    public void Configure(IEndpointGroupConfiguration config)
    {
        config.Prefix("/secured").RequireAuthorization();
    }
}

public class SecuredResourceEndpoint : IEndpoint<Response<HelloResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Get("/resource").Group<SecuredGroup>();
    }

    public Task<Response<HelloResponse>> HandleAsync(CancellationToken cancel) =>
        Task.FromResult(new Response<HelloResponse> { Body = new HelloResponse("secured") });
}

public class OverrideGroup : IEndpointGroup
{
    public void Configure(IEndpointGroupConfiguration config)
    {
        config.Prefix("/override").RequireAuthorization();
    }
}

public class OverrideAnonymousEndpoint : IEndpoint<Response<HelloResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        // Explicitly overrides the group's RequireAuthorization() for this endpoint only.
        config.Get("/anonymous").Group<OverrideGroup>().AllowAnonymous();
    }

    public Task<Response<HelloResponse>> HandleAsync(CancellationToken cancel) =>
        Task.FromResult(new Response<HelloResponse> { Body = new HelloResponse("override") });
}

public class OverrideInheritedEndpoint : IEndpoint<Response<HelloResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        // Declares no authorization setting of its own, so the group's applies.
        config.Get("/inherited").Group<OverrideGroup>();
    }

    public Task<Response<HelloResponse>> HandleAsync(CancellationToken cancel) =>
        Task.FromResult(new Response<HelloResponse> { Body = new HelloResponse("inherited") });
}

public class AuthorizationWebApplicationFactory : IAsyncLifetime, IDisposable
{
    private WebApplication? _app;
    private readonly Assembly _testAssembly = typeof(ProtectedEndpoint).Assembly;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder
            .Services.AddAuthentication(NoResultAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, NoResultAuthenticationHandler>(
                NoResultAuthenticationHandler.SchemeName,
                configureOptions: null
            );
        builder.Services.AddAuthorization();
        builder.Services.AddAxisEndpoints(_testAssembly);

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
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

// Authentication scheme that never establishes a principal, so every request stays anonymous.
// This isolates authorization behavior: endpoints that require an authenticated user return 401.
internal sealed class NoResultAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    internal const string SchemeName = "Test";

    public NoResultAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder
    )
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(AuthenticateResult.NoResult());
}
