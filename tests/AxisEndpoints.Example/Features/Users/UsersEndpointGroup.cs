using AxisEndpoints;
using AxisEndpoints.Example.Filters;

namespace AxisEndpoints.Example.Features.Users;

/// <summary>
/// Groups all /api/users endpoints under a shared prefix, tags, and logging filter.
/// Demonstrates IEndpointGroup with group-level AddFilter.
/// </summary>
public class UsersEndpointGroup : IEndpointGroup
{
    public void Configure(IEndpointGroupConfiguration config)
    {
        config.Prefix("/api/users").Tags("Users").AddFilter<LoggingFilter>();
    }
}
