using AxisEndpoints;
using AxisEndpoints.Example.Filters;

namespace AxisEndpoints.Example.Features.Admin;

/// <summary>
/// Groups all /admin endpoints.
/// Demonstrates RequireAuthorization(Action&lt;AuthorizationPolicyBuilder&gt;) — a dynamically
/// constructed policy. AllowAnonymous is used here so the example runs without credentials.
/// In a real application, replace AllowAnonymous with:
///   config.RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Admin"))
/// </summary>
public class AdminEndpointGroup : IEndpointGroup
{
    public void Configure(IEndpointGroupConfiguration config)
    {
        config.Prefix("/admin").Tags("Admin").AddFilter<LoggingFilter>().AllowAnonymous();
    }
}
