using Microsoft.AspNetCore.Authorization;

namespace AxisEndpoints.Internal;

internal sealed class EndpointGroupConfiguration : IEndpointGroupConfiguration
{
    internal string Prefix { get; private set; } = string.Empty;
    internal string[] Tags { get; private set; } = [];

    // Authorization state: a single value — last call wins.
    internal AuthorizationRequirement Authorization { get; private set; } =
        AuthorizationRequirement.Default;

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
        Authorization =
            roles.Length > 0
                ? new AuthorizationRequirement.Roles(roles)
                : new AuthorizationRequirement.AuthenticatedUser();
        return this;
    }

    IEndpointGroupConfiguration IEndpointGroupConfiguration.RequireAuthorization(string policy)
    {
        Authorization = new AuthorizationRequirement.NamedPolicy(policy);
        return this;
    }

    IEndpointGroupConfiguration IEndpointGroupConfiguration.RequireAuthorization(
        Action<AuthorizationPolicyBuilder> build
    )
    {
        Authorization = new AuthorizationRequirement.CustomPolicy(build);
        return this;
    }

    IEndpointGroupConfiguration IEndpointGroupConfiguration.AllowAnonymous()
    {
        Authorization = new AuthorizationRequirement.Anonymous();
        return this;
    }

    IEndpointGroupConfiguration IEndpointGroupConfiguration.AddFilter<TFilter>()
    {
        FilterTypes.Add(typeof(TFilter));
        return this;
    }
}
