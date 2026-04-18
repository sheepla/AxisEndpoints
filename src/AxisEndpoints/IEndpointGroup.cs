namespace AxisEndpoints;

/// <summary>
/// Defines a group of endpoints that share a common route prefix, tags, and authorization policy.
/// Implement this interface and reference it via <c>config.Group&lt;TGroup&gt;()</c> in an endpoint's Configure method.
/// </summary>
public interface IEndpointGroup
{
    void Configure(IEndpointGroupConfiguration config);
}
