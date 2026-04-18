namespace AxisEndpoints;

/// <summary>
/// Represents the HTTP methods supported by an endpoint.
/// Using an enum rather than string literals enables exhaustiveness checks in switch expressions.
/// </summary>
public enum HttpEndpointMethod
{
    Get,
    Post,
    Put,
    Patch,
    Delete,
    Head,
}
