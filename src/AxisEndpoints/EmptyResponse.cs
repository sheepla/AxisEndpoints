namespace AxisEndpoints;

/// <summary>
/// Marker type for endpoints that return no response body.
/// Use with <see cref="Response{TBody}"/>: <c>Response&lt;EmptyResponse&gt;</c>.
/// The framework skips JSON serialization when the body is this type.
/// Prefer the <see cref="Response.Empty"/> or <see cref="Response.NoContent"/> shorthands.
/// </summary>
public sealed class EmptyResponse
{
    public static readonly EmptyResponse Instance = new();

    private EmptyResponse() { }
}
