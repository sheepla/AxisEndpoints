using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AxisEndpoints.Internal;

internal sealed class EndpointConfiguration : IEndpointConfiguration
{
    internal string Route { get; private set; } = string.Empty;
    internal HttpEndpointMethod Method { get; private set; }
    internal string[] Tags { get; private set; } = [];
    internal string SummaryText { get; private set; } = string.Empty;
    internal string DescriptionText { get; private set; } = string.Empty;

    // Authorization state: a single value — last call wins.
    internal AuthorizationRequirement Authorization { get; private set; } =
        AuthorizationRequirement.Default;

    internal Type? GroupType { get; private set; }
    internal EndpointGroupConfiguration? GroupConfig { get; private set; }
    internal Type? ResponseType { get; set; }

    // Filter types are stored in registration order and applied during MapEndpoints.
    internal List<Type> FilterTypes { get; } = [];

    // Explicit OpenAPI response declarations from ProducesSuccess/ProducesError.
    // Used when HandleAsync returns IResult and the response schema cannot be inferred.
    // ContentType is null when the default (application/json) should be used.
    internal List<(int StatusCode, Type BodyType, string? ContentType)> ExtraProducesEntries { get; } =
        [];

    IEndpointConfiguration IEndpointConfiguration.Get([StringSyntax("Route")] string route) =>
        SetMethod(HttpEndpointMethod.Get, route);

    IEndpointConfiguration IEndpointConfiguration.Post([StringSyntax("Route")] string route) =>
        SetMethod(HttpEndpointMethod.Post, route);

    IEndpointConfiguration IEndpointConfiguration.Put([StringSyntax("Route")] string route) =>
        SetMethod(HttpEndpointMethod.Put, route);

    IEndpointConfiguration IEndpointConfiguration.Patch([StringSyntax("Route")] string route) =>
        SetMethod(HttpEndpointMethod.Patch, route);

    IEndpointConfiguration IEndpointConfiguration.Delete([StringSyntax("Route")] string route) =>
        SetMethod(HttpEndpointMethod.Delete, route);

    IEndpointConfiguration IEndpointConfiguration.Head([StringSyntax("Route")] string route) =>
        SetMethod(HttpEndpointMethod.Head, route);

    IEndpointConfiguration IEndpointConfiguration.Group<TGroup>()
    {
        var group = new TGroup();
        var groupConfig = new EndpointGroupConfiguration();
        group.Configure(groupConfig);
        GroupType = typeof(TGroup);
        GroupConfig = groupConfig;
        return this;
    }

    IEndpointConfiguration IEndpointConfiguration.AllowAnonymous()
    {
        Authorization = new AuthorizationRequirement.Anonymous();
        return this;
    }

    IEndpointConfiguration IEndpointConfiguration.RequireAuthorization(params string[] roles)
    {
        Authorization =
            roles.Length > 0
                ? new AuthorizationRequirement.Roles(roles)
                : new AuthorizationRequirement.AuthenticatedUser();
        return this;
    }

    IEndpointConfiguration IEndpointConfiguration.RequireAuthorization(string policy)
    {
        Authorization = new AuthorizationRequirement.NamedPolicy(policy);
        return this;
    }

    IEndpointConfiguration IEndpointConfiguration.RequireAuthorization(
        Action<AuthorizationPolicyBuilder> build
    )
    {
        Authorization = new AuthorizationRequirement.CustomPolicy(build);
        return this;
    }

    IEndpointConfiguration IEndpointConfiguration.Tags(params string[] tags)
    {
        Tags = tags;
        return this;
    }

    IEndpointConfiguration IEndpointConfiguration.Summary(string summary)
    {
        SummaryText = summary;
        return this;
    }

    IEndpointConfiguration IEndpointConfiguration.Description(string description)
    {
        DescriptionText = description;
        return this;
    }

    IEndpointConfiguration IEndpointConfiguration.ProducesSuccess<TBody>(
        HttpStatusCode statusCode,
        string? contentType
    )
    {
        ExtraProducesEntries.Add(((int)statusCode, typeof(TBody), contentType));
        return this;
    }

    IEndpointConfiguration IEndpointConfiguration.ProducesError(
        HttpStatusCode statusCode,
        string? contentType
    )
    {
        ExtraProducesEntries.Add(((int)statusCode, typeof(ProblemDetails), contentType));
        return this;
    }

    IEndpointConfiguration IEndpointConfiguration.ProducesError<TError>(
        HttpStatusCode statusCode,
        string? contentType
    )
    {
        ExtraProducesEntries.Add(((int)statusCode, typeof(TError), contentType));
        return this;
    }

    IEndpointConfiguration IEndpointConfiguration.AddFilter<TFilter>()
    {
        FilterTypes.Add(typeof(TFilter));
        return this;
    }

    private EndpointConfiguration SetMethod(HttpEndpointMethod method, string route)
    {
        Method = method;
        Route = route;
        return this;
    }
}
