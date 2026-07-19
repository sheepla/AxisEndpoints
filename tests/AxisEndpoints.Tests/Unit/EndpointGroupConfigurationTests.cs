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
    public void AllowAnonymous_SetsAnonymousRequirement()
    {
        _config.AllowAnonymous();

        Internal.Authorization.Should().BeOfType<AuthorizationRequirement.Anonymous>();
    }

    [Fact]
    public void RequireAuthorization_WithRoles_SetsRolesRequirement()
    {
        _config.RequireAuthorization("Admin", "User");

        Internal
            .Authorization.Should()
            .BeOfType<AuthorizationRequirement.Roles>()
            .Which.Names.Should()
            .BeEquivalentTo("Admin", "User");
    }

    [Fact]
    public void RequireAuthorization_WithoutRoles_RequiresAuthenticatedUser()
    {
        _config.RequireAuthorization();

        Internal.Authorization.Should().BeOfType<AuthorizationRequirement.AuthenticatedUser>();
    }

    [Fact]
    public void RequireAuthorization_WithPolicy_SetsNamedPolicyRequirement()
    {
        _config.RequireAuthorization("PolicyName");

        Internal
            .Authorization.Should()
            .BeOfType<AuthorizationRequirement.NamedPolicy>()
            .Which.Name.Should()
            .Be("PolicyName");
    }

    [Fact]
    public void RequireAuthorization_WithBuilder_SetsCustomPolicyRequirement()
    {
        Action<AuthorizationPolicyBuilder> builder = b => b.RequireRole("Admin");

        _config.RequireAuthorization(builder);

        Internal
            .Authorization.Should()
            .BeOfType<AuthorizationRequirement.CustomPolicy>()
            .Which.Build.Should()
            .BeSameAs(builder);
    }

    [Fact]
    public void Authorization_DefaultsToUnspecified()
    {
        Internal.Authorization.Should().BeOfType<AuthorizationRequirement.Unspecified>();
    }

    [Fact]
    public void AddFilter_AddsToFilterTypes()
    {
        _config.AddFilter<DummyFilter>();

        Internal.FilterTypes.Should().ContainSingle().Which.Should().Be(typeof(DummyFilter));
    }

    private sealed class DummyFilter : IEndpointFilter
    {
        public ValueTask<object?> InvokeAsync(
            EndpointFilterInvocationContext context,
            EndpointFilterDelegate next
        ) => next(context);
    }
}
