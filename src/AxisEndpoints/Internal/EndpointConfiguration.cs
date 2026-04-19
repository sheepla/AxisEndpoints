using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;

namespace AxisEndpoints.Internal;

internal sealed class EndpointConfiguration : IEndpointConfiguration
{
    internal string Route { get; private set; } = string.Empty;
    internal HttpEndpointMethod Method { get; private set; }
    internal string[] Tags { get; private set; } = [];
    internal string SummaryText { get; private set; } = string.Empty;
    internal string DescriptionText { get; private set; } = string.Empty;
    internal bool IsAnonymousAllowed { get; private set; }

    // Authorization state: mutually exclusive — last call wins.
    // Roles, PolicyName, and PolicyBuilder correspond to the three RequireAuthorization overloads.
    internal string[] Roles { get; private set; } = [];
    internal string? PolicyName { get; private set; }
    internal Action<AuthorizationPolicyBuilder>? PolicyBuilder { get; private set; }

    internal Type? GroupType { get; private set; }
    internal EndpointGroupConfiguration? GroupConfig { get; private set; }
    internal Type? ResponseType { get; set; }

    // Filter types are stored in registration order and applied during MapEndpoints.
    internal List<Type> FilterTypes { get; } = [];

    IEndpointConfiguration IEndpointConfiguration.Get([StringSyntax("Route")] string route) =>
        SetMethod(HttpEndpointMethod.Get, route);

    IEndpointConfiguration IEndpointConfiguration.Post([StringSyntax("Route")] string route) =>
        SetMethod(HttpEndpointMethod.Post, route);

    IEndpointConfiguration IEndpointConfiguration.Put([StringSyntax("Route")] string route) =>
        SetMethod(HttpEndpointMethod.Put, route);

    IEndpointConfiguration IEndpointConfiguration.Patch([StringSyntax("Route")] string route) =>
        SetMethod(HttpEndpointMethod.Patch, route);

    IEndpointConfiguration IEndpointConfiguration.Delete([StringSyntax("Route")] string route) =>
        SetMethod(HttpEndpointMethod.Delete, route);

    IEndpointConfiguration IEndpointConfiguration.Head([StringSyntax("Route")] string route) =>
        SetMethod(HttpEndpointMethod.Head, route);

    IEndpointConfiguration IEndpointConfiguration.Group<TGroup>()
    {
        var group = new TGroup();
        var groupConfig = new EndpointGroupConfiguration();
        group.Configure(groupConfig);
        GroupType = typeof(TGroup);
        GroupConfig = groupConfig;
        return this;
    }

    IEndpointConfiguration IEndpointConfiguration.AllowAnonymous()
    {
        IsAnonymousAllowed = true;
        return this;
    }

    IEndpointConfiguration IEndpointConfiguration.RequireAuthorization(params string[] roles)
    {
        IsAnonymousAllowed = false;
        Roles = roles;
        PolicyName = null;
        PolicyBuilder = null;
        return this;
    }

    IEndpointConfiguration IEndpointConfiguration.RequireAuthorization(string policy)
    {
        IsAnonymousAllowed = false;
        PolicyName = policy;
        Roles = [];
        PolicyBuilder = null;
        return this;
    }

    IEndpointConfiguration IEndpointConfiguration.RequireAuthorization(
        Action<AuthorizationPolicyBuilder> build
    )
    {
        IsAnonymousAllowed = false;
        PolicyBuilder = build;
        Roles = [];
        PolicyName = null;
        return this;
    }

    IEndpointConfiguration IEndpointConfiguration.Tags(params string[] tags)
    {
        Tags = tags;
        return this;
    }

    IEndpointConfiguration IEndpointConfiguration.Summary(string summary)
    {
        SummaryText = summary;
        return this;
    }

    IEndpointConfiguration IEndpointConfiguration.Description(string description)
    {
        DescriptionText = description;
        return this;
    }

    IEndpointConfiguration IEndpointConfiguration.AddFilter<TFilter>()
    {
        FilterTypes.Add(typeof(TFilter));
        return this;
    }

    private EndpointConfiguration SetMethod(HttpEndpointMethod method, string route)
    {
        Method = method;
        Route = route;
        return this;
    }
}
