using AxisEndpoints.Internal;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace AxisEndpoints.Tests.Unit;

public class EndpointGroupConfigurationTests
{
    private readonly IEndpointGroupConfiguration _config = new EndpointGroupConfiguration();

    private EndpointGroupConfiguration Internal => (EndpointGroupConfiguration)_config;

    [Fact]
    public void Prefix_SetsPrefixProperty()
    {
        _config.Prefix("/api");

        Internal.Prefix.Should().Be("/api");
    }

    [Fact]
    public void Tags_SetsTagsProperty()
    {
        _config.Tags("tag1", "tag2");

        Internal.Tags.Should().BeEquivalentTo("tag1", "tag2");
    }

    [Fact]
    public void AllowAnonymous_SetsIsAnonymousAllowed()
    {
        _config.AllowAnonymous();

        Internal.IsAnonymousAllowed.Should().BeTrue();
    }

    [Fact]
    public void RequireAuthorization_WithRoles_SetsRoles()
    {
        _config.RequireAuthorization("Admin", "User");

        Internal.Roles.Should().BeEquivalentTo("Admin", "User");
        Internal.IsAnonymousAllowed.Should().BeFalse();
        Internal.PolicyName.Should().BeNull();
        Internal.PolicyBuilder.Should().BeNull();
    }

    [Fact]
    public void RequireAuthorization_WithPolicy_SetsPolicyName()
    {
        _config.RequireAuthorization("PolicyName");

        Internal.PolicyName.Should().Be("PolicyName");
        Internal.Roles.Should().BeEmpty();
        Internal.PolicyBuilder.Should().BeNull();
        Internal.IsAnonymousAllowed.Should().BeFalse();
    }

    [Fact]
    public void RequireAuthorization_WithBuilder_SetsPolicyBuilder()
    {
        Action<AuthorizationPolicyBuilder> builder = b => b.RequireRole("Admin");

        _config.RequireAuthorization(builder);

        Internal.PolicyBuilder.Should().BeSameAs(builder);
        Internal.Roles.Should().BeEmpty();
        Internal.PolicyName.Should().BeNull();
        Internal.IsAnonymousAllowed.Should().BeFalse();
    }

    [Fact]
    public void AddFilter_AddsToFilterTypes()
    {
        _config.AddFilter<DummyFilter>();

        Internal.FilterTypes.Should().ContainSingle()
            .Which.Should().Be(typeof(DummyFilter));
    }

    private sealed class DummyFilter : IEndpointFilter
    {
        public ValueTask<object?> InvokeAsync(
            EndpointFilterInvocationContext context,
            EndpointFilterDelegate next) => next(context);
    }
}
