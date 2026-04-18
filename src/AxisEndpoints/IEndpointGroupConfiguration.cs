using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace AxisEndpoints;

/// <summary>
/// Configures shared settings for a group of endpoints.
/// Settings defined here are applied to all endpoints in the group
/// and can be overridden per endpoint in <see cref="IEndpointConfiguration"/>.
/// </summary>
public interface IEndpointGroupConfiguration
{
    IEndpointGroupConfiguration Prefix(string prefix);
    IEndpointGroupConfiguration Tags(params string[] tags);
    IEndpointGroupConfiguration AllowAnonymous();

    /// <summary>Requires the authenticated user to be in at least one of the specified roles.</summary>
    IEndpointGroupConfiguration RequireAuthorization(params string[] roles);

    /// <summary>Requires the request to satisfy a named authorization policy defined via <c>AddAuthorization</c>.</summary>
    IEndpointGroupConfiguration RequireAuthorization(string policy);

    /// <summary>Requires the request to satisfy a dynamically constructed authorization policy.</summary>
    IEndpointGroupConfiguration RequireAuthorization(Action<AuthorizationPolicyBuilder> build);

    // Filters applied to all endpoints in this group — applied in registration order, outermost first
    IEndpointGroupConfiguration AddFilter<TFilter>()
        where TFilter : IEndpointFilter;
}
