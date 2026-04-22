using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AxisEndpoints.Internal;

internal static class EndpointRegistry
{
    internal static void RegisterServices(
        IServiceCollection services,
        IEnumerable<Assembly> assemblies
    )
    {
        var assemblyList = assemblies.ToList();
        var endpointTypes = assemblyList.SelectMany(a => a.GetTypes()).Where(IsEndpointType);

        foreach (var type in endpointTypes)
        {
            services.AddScoped(type);
        }

        // Collect all IEndpointFilter implementations in the scanned assemblies and register
        // them as scoped so AddFilter<TFilter>() can resolve them from DI at request time.
        var filterTypes = assemblyList
            .SelectMany(a => a.GetTypes())
            .Where(t =>
                !t.IsAbstract && !t.IsInterface && typeof(IEndpointFilter).IsAssignableFrom(t)
            );

        foreach (var filterType in filterTypes)
        {
            services.AddScoped(filterType);
        }
    }

    internal static void MapEndpoints(
        WebApplication app,
        IEnumerable<Assembly> assemblies,
        AxisEndpointsOptions options
    )
    {
        var endpointTypes = assemblies.SelectMany(a => a.GetTypes()).Where(IsEndpointType);
        var groupBuilders = new Dictionary<Type, RouteGroupBuilder>();

        foreach (var type in endpointTypes)
        {
            var (config, groupType) = ResolveConfiguration(type);

            if (string.IsNullOrEmpty(config.Route))
            {
                throw new InvalidOperationException(
                    $"{type.Name}.Configure() must specify an HTTP method and route."
                );
            }

            var routeBuilder = groupType is not null
                ? GetOrCreateGroupBuilder(app, groupBuilders, groupType, config.GroupConfig!)
                : (IEndpointRouteBuilder)app;

            var routeHandlerBuilder = MapRoute(routeBuilder, type, config, options);
            ApplyMetadata(routeHandlerBuilder, config);
        }
    }

    private static bool IsEndpointType(Type type)
    {
        if (type.IsAbstract || type.IsInterface)
        {
            return false;
        }

        return type.GetInterfaces()
            .Any(i =>
                i.IsGenericType
                && (
                    i.GetGenericTypeDefinition() == typeof(IEndpoint<,>)
                    || i.GetGenericTypeDefinition() == typeof(IEndpoint<>)
                )
            );
    }

    /// <summary>
    /// Calls Configure() on the endpoint type to read its routing and metadata declarations.
    /// Uses GetUninitializedObject so that endpoints with constructor-injected dependencies
    /// do not require a parameterless constructor. Configure() must not access any fields
    /// set by the constructor — it should only call methods on the IEndpointConfiguration argument.
    /// </summary>
    private static (EndpointConfiguration Config, Type? GroupType) ResolveConfiguration(
        Type endpointType
    )
    {
        var instance = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(
            endpointType
        );

        var config = new EndpointConfiguration();

        var configureMethod =
            endpointType.GetMethod(nameof(IEndpoint<object>.Configure))
            ?? throw new InvalidOperationException(
                $"Configure method not found on {endpointType.Name}."
            );

        configureMethod.Invoke(instance, [config]);

        return (config, config.GroupType);
    }

    private static RouteGroupBuilder GetOrCreateGroupBuilder(
        WebApplication app,
        Dictionary<Type, RouteGroupBuilder> cache,
        Type groupType,
        EndpointGroupConfiguration groupConfig
    )
    {
        if (cache.TryGetValue(groupType, out var existing))
        {
            return existing;
        }

        var groupBuilder = app.MapGroup(groupConfig.Prefix);
        ApplyGroupMetadata(groupBuilder, groupConfig);
        cache[groupType] = groupBuilder;
        return groupBuilder;
    }

    private static RouteHandlerBuilder MapRoute(
        IEndpointRouteBuilder builder,
        Type endpointType,
        EndpointConfiguration config,
        AxisEndpointsOptions options
    )
    {
        var twoParam = endpointType
            .GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEndpoint<,>)
            );

        if (twoParam is not null)
        {
            var args = twoParam.GetGenericArguments();
            return MapTypedRoute(
                builder,
                endpointType,
                config,
                requestType: args[0],
                resultType: args[1],
                options: options
            );
        }

        var oneParam = endpointType
            .GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEndpoint<>));

        return MapNoRequestRoute(
            builder,
            endpointType,
            config,
            resultType: oneParam.GetGenericArguments()[0]
        );
    }

    private static RouteHandlerBuilder MapTypedRoute(
        IEndpointRouteBuilder builder,
        Type endpointType,
        EndpointConfiguration config,
        Type requestType,
        Type resultType,
        AxisEndpointsOptions options
    )
    {
        var usesBody =
            config.Method
            is HttpEndpointMethod.Post
                or HttpEndpointMethod.Put
                or HttpEndpointMethod.Patch;

        // POST/PUT/PATCH: bind TRequest from the JSON body.
        // GET/DELETE/HEAD: pass TRequest via [AsParameters] so Minimal API expands its
        //   properties into individual route/query parameters for OpenAPI metadata generation.
        //   Types that define BindAsync fall back to (HttpContext, CancellationToken) binding.
        var handler = usesBody
            ? MakeBodyHandler(endpointType, requestType, resultType)
            : MakeParameterHandler(endpointType, requestType, resultType);

        var routeHandlerBuilder = config.Method switch
        {
            HttpEndpointMethod.Get => builder.MapGet(config.Route, handler),
            HttpEndpointMethod.Post => builder.MapPost(config.Route, handler),
            HttpEndpointMethod.Put => builder.MapPut(config.Route, handler),
            HttpEndpointMethod.Patch => builder.MapPatch(config.Route, handler),
            HttpEndpointMethod.Delete => builder.MapDelete(config.Route, handler),
            HttpEndpointMethod.Head => builder.MapMethods(config.Route, ["HEAD"], handler),
            _ => throw new InvalidOperationException($"Unsupported HTTP method: {config.Method}"),
        };

        if (!options.DisableDataAnnotationsValidation)
        {
            // Constructed inline because DataAnnotationsValidationFilter<TRequest> is a closed
            // generic type that cannot be registered in the DI container generically.
            var filterType = typeof(DataAnnotationsValidationFilter<>).MakeGenericType(requestType);
            var filter = (IEndpointFilter)Activator.CreateInstance(filterType)!;
            routeHandlerBuilder.AddEndpointFilter(filter);
        }

        config.ResponseType = resultType;
        return routeHandlerBuilder;
    }

    private static RouteHandlerBuilder MapNoRequestRoute(
        IEndpointRouteBuilder builder,
        Type endpointType,
        EndpointConfiguration config,
        Type resultType
    )
    {
        var handler = MakeNoRequestHandler(endpointType, resultType);

        var routeHandlerBuilder = config.Method switch
        {
            HttpEndpointMethod.Get => builder.MapGet(config.Route, handler),
            HttpEndpointMethod.Post => builder.MapPost(config.Route, handler),
            HttpEndpointMethod.Put => builder.MapPut(config.Route, handler),
            HttpEndpointMethod.Patch => builder.MapPatch(config.Route, handler),
            HttpEndpointMethod.Delete => builder.MapDelete(config.Route, handler),
            HttpEndpointMethod.Head => builder.MapMethods(config.Route, ["HEAD"], handler),
            _ => throw new InvalidOperationException($"Unsupported HTTP method: {config.Method}"),
        };

        config.ResponseType = resultType;
        return routeHandlerBuilder;
    }

    // --- Delegate factories ---

    private static Delegate MakeBodyHandler(Type endpointType, Type requestType, Type resultType) =>
        (Delegate)
            typeof(EndpointRegistry)
                .GetMethod(nameof(CreateBodyHandler), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(endpointType, requestType, resultType)
                .Invoke(null, [])!;

    private static Delegate MakeParameterHandler(
        Type endpointType,
        Type requestType,
        Type resultType
    )
    {
        // Types with BindAsync handle their own binding — wrap in a context-only handler.
        var hasBindAsync =
            requestType.GetMethod(
                "BindAsync",
                BindingFlags.Public | BindingFlags.Static,
                [typeof(HttpContext)]
            )
            is not null;

        if (hasBindAsync)
        {
            return (Delegate)
                typeof(EndpointRegistry)
                    .GetMethod(
                        nameof(CreateBindAsyncHandler),
                        BindingFlags.NonPublic | BindingFlags.Static
                    )!
                    .MakeGenericMethod(endpointType, requestType, resultType)
                    .Invoke(null, [])!;
        }

        // Use [AsParameters] handler: Minimal API expands TRequest properties into individual
        // route/query parameters, preserving [FromRoute]/[FromQuery] attributes on each property.
        return (Delegate)
            typeof(EndpointRegistry)
                .GetMethod(
                    nameof(CreateAsParametersHandler),
                    BindingFlags.NonPublic | BindingFlags.Static
                )!
                .MakeGenericMethod(endpointType, requestType, resultType)
                .Invoke(null, [])!;
    }

    private static Delegate MakeNoRequestHandler(Type endpointType, Type resultType) =>
        (Delegate)
            typeof(EndpointRegistry)
                .GetMethod(
                    nameof(CreateNoRequestHandler),
                    BindingFlags.NonPublic | BindingFlags.Static
                )!
                .MakeGenericMethod(endpointType, resultType)
                .Invoke(null, [])!;

    // --- Result conversion ---

    /// <summary>
    /// Converts the value returned from HandleAsync to an IResult.
    /// TResult is either Response&lt;TBody&gt; (serialized as JSON) or an IResult implementation
    /// (executed directly). The branch is a static type check resolved once per endpoint
    /// registration, not on every request.
    /// </summary>
    private static IResult ToIResult<TResult>(TResult result)
    {
        if (result is IResult directResult)
        {
            return directResult;
        }

        // TResult is Response<TBody>: unwrap via the open-generic ToResult method.
        // This avoids reflection on the hot path by resolving the method once at registration time.
        // Called rarely enough that the cast is acceptable here.
        var resultType = typeof(TResult);
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Response<>))
        {
            var bodyType = resultType.GetGenericArguments()[0];
            var toResult = typeof(ResponseExecutor)
                .GetMethod(
                    nameof(ResponseExecutor.ToResult),
                    BindingFlags.NonPublic | BindingFlags.Static
                )!
                .MakeGenericMethod(bodyType);
            return (IResult)toResult.Invoke(null, [result])!;
        }

        throw new InvalidOperationException(
            $"HandleAsync returned an unsupported type '{resultType.Name}'. "
                + $"Return Response<TBody> for JSON responses or an IResult implementation for custom responses."
        );
    }

    // --- Concrete handler implementations ---

    /// <summary>
    /// POST/PUT/PATCH: TRequest bound from the JSON body.
    /// Returns Task&lt;IResult&gt; so Minimal API infers TResult for OpenAPI response schema.
    /// </summary>
    private static Func<TRequest, HttpContext, CancellationToken, Task<IResult>> CreateBodyHandler<
        TEndpoint,
        TRequest,
        TResult
    >()
        where TEndpoint : class, IEndpoint<TRequest, TResult>
    {
        return async (request, context, cancel) =>
        {
            var endpoint = context.RequestServices.GetRequiredService<TEndpoint>();
            var result = await endpoint.HandleAsync(request, cancel);
            return ToIResult(result);
        };
    }

    /// <summary>
    /// GET/DELETE/HEAD: [AsParameters] causes Minimal API to expand TRequest properties
    /// into individual parameters, generating correct OpenAPI route/query parameter metadata
    /// while preserving [FromRoute] and [FromQuery] attributes.
    /// </summary>
    private static Func<
        TRequest,
        HttpContext,
        CancellationToken,
        Task<IResult>
    > CreateAsParametersHandler<TEndpoint, TRequest, TResult>()
        where TEndpoint : class, IEndpoint<TRequest, TResult>
    {
        return async ([AsParameters] request, context, cancel) =>
        {
            var endpoint = context.RequestServices.GetRequiredService<TEndpoint>();
            var result = await endpoint.HandleAsync(request, cancel);
            return ToIResult(result);
        };
    }

    /// <summary>
    /// GET/DELETE/HEAD with BindAsync: the type handles its own binding from HttpContext.
    /// BindAsync(HttpContext) — the single-parameter overload — is used here.
    /// OpenAPI parameter metadata is not inferred for these — document manually if needed.
    /// </summary>
    private static Func<HttpContext, CancellationToken, Task<IResult>> CreateBindAsyncHandler<
        TEndpoint,
        TRequest,
        TResult
    >()
        where TEndpoint : class, IEndpoint<TRequest, TResult>
    {
        return async (context, cancel) =>
        {
            var bindMethod = typeof(TRequest).GetMethod(
                "BindAsync",
                BindingFlags.Public | BindingFlags.Static,
                [typeof(HttpContext)]
            )!;

            var task = (ValueTask<TRequest>)bindMethod.Invoke(null, [context])!;
            var request = await task;

            var endpoint = context.RequestServices.GetRequiredService<TEndpoint>();
            var result = await endpoint.HandleAsync(request, cancel);
            return ToIResult(result);
        };
    }

    /// <summary>
    /// IEndpoint&lt;TResult&gt;: no request parameters at all.
    /// </summary>
    private static Func<HttpContext, CancellationToken, Task<IResult>> CreateNoRequestHandler<
        TEndpoint,
        TResult
    >()
        where TEndpoint : class, IEndpoint<TResult>
    {
        return async (context, cancel) =>
        {
            var endpoint = context.RequestServices.GetRequiredService<TEndpoint>();
            var result = await endpoint.HandleAsync(cancel);
            return ToIResult(result);
        };
    }

    // --- Metadata helpers ---

    private static void ApplyMetadata(
        RouteHandlerBuilder routeBuilder,
        EndpointConfiguration config
    )
    {
        if (config.Tags.Length > 0)
        {
            routeBuilder.WithTags(config.Tags);
        }

        if (!string.IsNullOrEmpty(config.SummaryText))
        {
            routeBuilder.WithSummary(config.SummaryText);
        }

        if (!string.IsNullOrEmpty(config.DescriptionText))
        {
            routeBuilder.WithDescription(config.DescriptionText);
        }

        if (config.IsAnonymousAllowed)
        {
            routeBuilder.AllowAnonymous();
        }
        else if (config.PolicyBuilder is not null)
        {
            routeBuilder.RequireAuthorization(config.PolicyBuilder);
        }
        else if (config.PolicyName is not null)
        {
            routeBuilder.RequireAuthorization(config.PolicyName);
        }
        else if (config.Roles.Length > 0)
        {
            routeBuilder.RequireAuthorization(policy => policy.RequireRole(config.Roles));
        }

        foreach (var filterType in config.FilterTypes)
        {
            routeBuilder.AddEndpointFilter(
                async (context, next) =>
                {
                    var filter = (IEndpointFilter)
                        context.HttpContext.RequestServices.GetRequiredService(filterType);
                    return await filter.InvokeAsync(context, next);
                }
            );
        }

        if (config.ResponseType is not null)
        {
            routeBuilder.Produces(200, config.ResponseType);
        }
    }

    private static void ApplyGroupMetadata(
        RouteGroupBuilder groupBuilder,
        EndpointGroupConfiguration config
    )
    {
        if (config.Tags.Length > 0)
        {
            groupBuilder.WithTags(config.Tags);
        }

        if (config.IsAnonymousAllowed)
        {
            groupBuilder.AllowAnonymous();
        }
        else if (config.PolicyBuilder is not null)
        {
            groupBuilder.RequireAuthorization(config.PolicyBuilder);
        }
        else if (config.PolicyName is not null)
        {
            groupBuilder.RequireAuthorization(config.PolicyName);
        }
        else if (config.Roles.Length > 0)
        {
            groupBuilder.RequireAuthorization(policy => policy.RequireRole(config.Roles));
        }

        foreach (var filterType in config.FilterTypes)
        {
            groupBuilder.AddEndpointFilter(
                async (context, next) =>
                {
                    var filter = (IEndpointFilter)
                        context.HttpContext.RequestServices.GetRequiredService(filterType);
                    return await filter.InvokeAsync(context, next);
                }
            );
        }
    }
}
