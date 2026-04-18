using Microsoft.AspNetCore.Authorization;

namespace AxisEndpoints.Internal;

internal sealed class EndpointGroupConfiguration : IEndpointGroupConfiguration
{
    internal string Prefix { get; private set; } = string.Empty;
    internal string[] Tags { get; private set; } = [];
    internal bool IsAnonymousAllowed { get; private set; }

    // Authorization state: mutually exclusive — last call wins.
    internal string[] Roles { get; private set; } = [];
    internal string? PolicyName { get; private set; }
    internal Action<AuthorizationPolicyBuilder>? PolicyBuilder { get; private set; }

    internal List<Type> FilterTypes { get; } = [];

    IEndpointGroupConfiguration IEndpointGroupConfiguration.Prefix(string prefix)
    {
        Prefix = prefix;
        return this;
    }

    IEndpointGroupConfiguration IEndpointGroupConfiguration.Tags(params string[] tags)
    {
        Tags = tags;
        return this;
    }

    IEndpointGroupConfiguration IEndpointGroupConfiguration.RequireAuthorization(
        params string[] roles
    )
    {
        IsAnonymousAllowed = false;
        Roles = roles;
        PolicyName = null;
        PolicyBuilder = null;
        return this;
    }

    IEndpointGroupConfiguration IEndpointGroupConfiguration.RequireAuthorization(string policy)
    {
        IsAnonymousAllowed = false;
        PolicyName = policy;
        Roles = [];
        PolicyBuilder = null;
        return this;
    }

    IEndpointGroupConfiguration IEndpointGroupConfiguration.RequireAuthorization(
        Action<AuthorizationPolicyBuilder> build
    )
    {
        IsAnonymousAllowed = false;
        PolicyBuilder = build;
        Roles = [];
        PolicyName = null;
        return this;
    }

    IEndpointGroupConfiguration IEndpointGroupConfiguration.AllowAnonymous()
    {
        IsAnonymousAllowed = true;
        return this;
    }

    IEndpointGroupConfiguration IEndpointGroupConfiguration.AddFilter<TFilter>()
    {
        FilterTypes.Add(typeof(TFilter));
        return this;
    }
}
