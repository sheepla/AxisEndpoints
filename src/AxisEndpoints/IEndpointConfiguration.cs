using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace AxisEndpoints;

/// <summary>
/// Configures routing, metadata, authorization, and filters for an endpoint.
/// Call one of the HTTP method methods first, then chain additional configuration.
/// </summary>
public interface IEndpointConfiguration
{
    // Route and HTTP method — [StringSyntax("Route")] enables route parameter completion in IDEs
    IEndpointConfiguration Get([StringSyntax("Route")] string route);
    IEndpointConfiguration Post([StringSyntax("Route")] string route);
    IEndpointConfiguration Put([StringSyntax("Route")] string route);
    IEndpointConfiguration Patch([StringSyntax("Route")] string route);
    IEndpointConfiguration Delete([StringSyntax("Route")] string route);
    IEndpointConfiguration Head([StringSyntax("Route")] string route);

    // Group
    IEndpointConfiguration Group<TGroup>()
        where TGroup : IEndpointGroup, new();

    // Authorization
    IEndpointConfiguration AllowAnonymous();

    /// <summary>Requires the authenticated user to be in at least one of the specified roles.</summary>
    IEndpointConfiguration RequireAuthorization(params string[] roles);

    /// <summary>Requires the request to satisfy a named authorization policy defined via <c>AddAuthorization</c>.</summary>
    IEndpointConfiguration RequireAuthorization(string policy);

    /// <summary>Requires the request to satisfy a dynamically constructed authorization policy.</summary>
    IEndpointConfiguration RequireAuthorization(Action<AuthorizationPolicyBuilder> build);

    // OpenAPI metadata
    IEndpointConfiguration Tags(params string[] tags);
    IEndpointConfiguration Summary(string summary);
    IEndpointConfiguration Description(string description);

    // OpenAPI response metadata for IResult-returning endpoints.
    // When HandleAsync returns Response<TBody>, the 200 schema is inferred automatically and
    // these methods are not needed. Use them when the return type is IResult to declare the
    // success and error response shapes explicitly.

    /// <summary>
    /// Declares a success response for OpenAPI. Use when HandleAsync returns <see cref="IResult"/>
    /// and the 200 schema cannot be inferred automatically.
    /// </summary>
    IEndpointConfiguration ProducesSuccess<TBody>(HttpStatusCode statusCode = HttpStatusCode.OK);

    /// <summary>
    /// Declares an error response with <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> as the body type.
    /// </summary>
    IEndpointConfiguration ProducesError(HttpStatusCode statusCode);

    /// <summary>
    /// Declares an error response with a custom body type.
    /// </summary>
    IEndpointConfiguration ProducesError<TError>(HttpStatusCode statusCode);

    // Filters — applied in registration order, outermost first
    IEndpointConfiguration AddFilter<TFilter>()
        where TFilter : IEndpointFilter;
}
