using AxisEndpoints.Extensions;
using AxisEndpoints.Tests.Integration;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AxisEndpoints.Tests.Unit;

public class ServiceRegistrationTests
{
    [Fact]
    public void AddAxisEndpoints_RegistersEndpointsAsScoped()
    {
        var services = new ServiceCollection();
        services.AddAxisEndpoints(typeof(HelloEndpoint).Assembly);
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<HelloEndpoint>().Should().NotBeNull();
    }

    [Fact]
    public void AddAxisEndpoints_RegistersEndpointContext()
    {
        var services = new ServiceCollection();
        services.AddAxisEndpoints(typeof(HelloEndpoint).Assembly);
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<EndpointContext>().Should().NotBeNull();
    }

    [Fact]
    public void AddAxisEndpoints_RegistersEndpointWithRequest()
    {
        var services = new ServiceCollection();
        services.AddAxisEndpoints(typeof(EchoEndpoint).Assembly);
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<EchoEndpoint>().Should().NotBeNull();
    }
}
