namespace AxisEndpoints;

/// <summary>
/// Options for configuring AxisEndpoints behavior at registration time.
/// Pass an <see cref="Action{AxisEndpointsOptions}"/> to
/// <c>AddAxisEndpoints(options => ...)</c> to customize.
/// </summary>
public sealed class AxisEndpointsOptions
{
    /// <summary>
    /// When <c>true</c>, the built-in DataAnnotations validation filter is not added to any endpoint.
    /// Defaults to <c>false</c> (validation is enabled by default).
    /// </summary>
    public bool DisableDataAnnotationsValidation { get; set; } = false;
}
