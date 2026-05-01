using System.Net;
using AxisEndpoints.Internal;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace AxisEndpoints.Tests.Unit;

public class EndpointConfigurationTests
{
    private readonly IEndpointConfiguration _config = new EndpointConfiguration();

    private EndpointConfiguration Internal => (EndpointConfiguration)_config;

    [Fact]
    public void Get_SetsRouteAndMethod()
    {
        _config.Get("/test");

        Internal.Route.Should().Be("/test");
        Internal.Method.Should().Be(HttpEndpointMethod.Get);
    }

    [Fact]
    public void Post_SetsRouteAndMethod()
    {
        _config.Post("/test");

        Internal.Route.Should().Be("/test");
        Internal.Method.Should().Be(HttpEndpointMethod.Post);
    }

    [Fact]
    public void Put_SetsRouteAndMethod()
    {
        _config.Put("/test");

        Internal.Route.Should().Be("/test");
        Internal.Method.Should().Be(HttpEndpointMethod.Put);
    }

    [Fact]
    public void Patch_SetsRouteAndMethod()
    {
        _config.Patch("/test");

        Internal.Route.Should().Be("/test");
        Internal.Method.Should().Be(HttpEndpointMethod.Patch);
    }

    [Fact]
    public void Delete_SetsRouteAndMethod()
    {
        _config.Delete("/test");

        Internal.Route.Should().Be("/test");
        Internal.Method.Should().Be(HttpEndpointMethod.Delete);
    }

    [Fact]
    public void Head_SetsRouteAndMethod()
    {
        _config.Head("/test");

        Internal.Route.Should().Be("/test");
        Internal.Method.Should().Be(HttpEndpointMethod.Head);
    }

    [Fact]
    public void Tags_SetsTagsProperty()
    {
        _config.Get("/test").Tags("tag1", "tag2");

        Internal.Tags.Should().BeEquivalentTo("tag1", "tag2");
    }

    [Fact]
    public void Summary_SetsSummaryText()
    {
        _config.Get("/test").Summary("summary text");

        Internal.SummaryText.Should().Be("summary text");
    }

    [Fact]
    public void Description_SetsDescriptionText()
    {
        _config.Get("/test").Description("description text");

        Internal.DescriptionText.Should().Be("description text");
    }

    [Fact]
    public void AllowAnonymous_SetsIsAnonymousAllowed()
    {
        _config.Get("/test").AllowAnonymous();

        Internal.IsAnonymousAllowed.Should().BeTrue();
    }

    [Fact]
    public void RequireAuthorization_WithRoles_SetsRolesAndClearsAnonymous()
    {
        _config.Get("/test").RequireAuthorization("Admin", "User");

        Internal.Roles.Should().BeEquivalentTo("Admin", "User");
        Internal.IsAnonymousAllowed.Should().BeFalse();
        Internal.PolicyName.Should().BeNull();
        Internal.PolicyBuilder.Should().BeNull();
    }

    [Fact]
    public void RequireAuthorization_WithPolicy_SetsPolicyNameAndClearsRoles()
    {
        _config.Get("/test").RequireAuthorization("PolicyName");

        Internal.PolicyName.Should().Be("PolicyName");
        Internal.Roles.Should().BeEmpty();
        Internal.PolicyBuilder.Should().BeNull();
        Internal.IsAnonymousAllowed.Should().BeFalse();
    }

    [Fact]
    public void RequireAuthorization_WithBuilder_SetsPolicyBuilderAndClearsOthers()
    {
        Action<AuthorizationPolicyBuilder> builder = b => b.RequireRole("Admin");

        _config.Get("/test").RequireAuthorization(builder);

        Internal.PolicyBuilder.Should().BeSameAs(builder);
        Internal.Roles.Should().BeEmpty();
        Internal.PolicyName.Should().BeNull();
        Internal.IsAnonymousAllowed.Should().BeFalse();
    }

    [Fact]
    public void RequireAuthorization_Policy_AfterRoles_ClearsRoles()
    {
        _config.Get("/test")
            .RequireAuthorization("Admin", "User")
            .RequireAuthorization("PolicyName");

        Internal.PolicyName.Should().Be("PolicyName");
        Internal.Roles.Should().BeEmpty();
    }

    [Fact]
    public void ProducesSuccess_AddsToExtraProducesEntries()
    {
        _config.Get("/test").ProducesSuccess<string>(HttpStatusCode.OK);

        Internal.ExtraProducesEntries.Should().ContainSingle()
            .Which.Should().Be((200, typeof(string)));
    }

    [Fact]
    public void ProducesError_AddsToExtraProducesEntries()
    {
        _config.Get("/test").ProducesError(HttpStatusCode.NotFound);

        Internal.ExtraProducesEntries.Should().ContainSingle()
            .Which.Should().Be((404, typeof(Microsoft.AspNetCore.Mvc.ProblemDetails)));
    }

    [Fact]
    public void AddFilter_AddsToFilterTypes()
    {
        _config.Get("/test").AddFilter<DummyFilter>();

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
