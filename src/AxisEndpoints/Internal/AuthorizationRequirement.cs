using Microsoft.AspNetCore.Authorization;

namespace AxisEndpoints.Internal;

/// <summary>
/// The authorization requirement declared for an endpoint or endpoint group.
/// Modeled as a closed hierarchy so the mutually exclusive states are represented
/// by a single value and an invalid combination cannot be constructed. The private
/// constructor keeps the set of cases closed to this file.
/// </summary>
internal abstract record AuthorizationRequirement
{
    private AuthorizationRequirement() { }

    internal static readonly AuthorizationRequirement Default = new Unspecified();

    /// <summary>No authorization was declared; the application's default (fallback) policy applies.</summary>
    internal sealed record Unspecified : AuthorizationRequirement;

    /// <summary>Access is allowed without authentication.</summary>
    internal sealed record Anonymous : AuthorizationRequirement;

    /// <summary>Requires an authenticated user without any role or named-policy constraint.</summary>
    internal sealed record AuthenticatedUser : AuthorizationRequirement;

    /// <summary>Requires the authenticated user to be in at least one of the roles.</summary>
    internal sealed record Roles(string[] Names) : AuthorizationRequirement;

    /// <summary>Requires the request to satisfy a named authorization policy.</summary>
    internal sealed record NamedPolicy(string Name) : AuthorizationRequirement;

    /// <summary>Requires the request to satisfy a dynamically constructed authorization policy.</summary>
    internal sealed record CustomPolicy(Action<AuthorizationPolicyBuilder> Build)
        : AuthorizationRequirement;
}
