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
                responseType: args[1],
                options: options
            );
        }

        var oneParam = endpointType
            .GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEndpoint<>));

        return MapResponseOnlyRoute(
            builder,
            endpointType,
            config,
            responseType: oneParam.GetGenericArguments()[0]
        );
    }

    private static RouteHandlerBuilder MapTypedRoute(
        IEndpointRouteBuilder builder,
        Type endpointType,
        EndpointConfiguration config,
        Type requestType,
        Type responseType,
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
            ? MakeBodyHandler(endpointType, requestType, responseType)
            : MakeParameterHandler(endpointType, requestType, responseType);

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

        config.ResponseType = responseType;
        return routeHandlerBuilder;
    }

    private static RouteHandlerBuilder MapResponseOnlyRoute(
        IEndpointRouteBuilder builder,
        Type endpointType,
        EndpointConfiguration config,
        Type responseType
    )
    {
        var handler = MakeNoRequestHandler(endpointType, responseType);

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

        config.ResponseType = responseType;
        return routeHandlerBuilder;
    }

    // --- Delegate factories ---

    private static Delegate MakeBodyHandler(
        Type endpointType,
        Type requestType,
        Type responseType
    ) =>
        (Delegate)
            typeof(EndpointRegistry)
                .GetMethod(nameof(CreateBodyHandler), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(endpointType, requestType, responseType)
                .Invoke(null, [])!;

    private static Delegate MakeParameterHandler(
        Type endpointType,
        Type requestType,
        Type responseType
    )
    {
        // Types with BindAsync handle their own binding — wrap in a context-only handler.
        var hasBindAsync =
            requestType.GetMethod(
                "BindAsync",
                BindingFlags.Public | BindingFlags.Static,
                [typeof(HttpContext), typeof(ParameterInfo)]
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
                    .MakeGenericMethod(endpointType, requestType, responseType)
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
                .MakeGenericMethod(endpointType, requestType, responseType)
                .Invoke(null, [])!;
    }

    private static Delegate MakeNoRequestHandler(Type endpointType, Type responseType) =>
        (Delegate)
            typeof(EndpointRegistry)
                .GetMethod(
                    nameof(CreateNoRequestHandler),
                    BindingFlags.NonPublic | BindingFlags.Static
                )!
                .MakeGenericMethod(endpointType, responseType)
                .Invoke(null, [])!;

    // --- Concrete handler implementations ---

    /// <summary>
    /// POST/PUT/PATCH: TRequest bound from the JSON body.
    /// Returns Task&lt;IResult&gt; so Minimal API infers TResponse for OpenAPI response schema.
    /// </summary>
    private static Func<TRequest, HttpContext, CancellationToken, Task<IResult>> CreateBodyHandler<
        TEndpoint,
        TRequest,
        TResponse
    >()
        where TEndpoint : class, IEndpoint<TRequest, TResponse>
    {
        return async (request, context, cancel) =>
        {
            var endpoint = context.RequestServices.GetRequiredService<TEndpoint>();
            var sender = new ResponseSender<TResponse>();
            await endpoint.HandleAsync(sender, request, cancel);
            return sender.Result ?? Results.Ok();
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
    > CreateAsParametersHandler<TEndpoint, TRequest, TResponse>()
        where TEndpoint : class, IEndpoint<TRequest, TResponse>
    {
        return async ([AsParameters] request, context, cancel) =>
        {
            var endpoint = context.RequestServices.GetRequiredService<TEndpoint>();
            var sender = new ResponseSender<TResponse>();
            await endpoint.HandleAsync(sender, request, cancel);
            return sender.Result ?? Results.Ok();
        };
    }

    /// <summary>
    /// GET/DELETE/HEAD with BindAsync: the type handles its own binding from HttpContext.
    /// OpenAPI parameter metadata is not inferred for these — document manually if needed.
    /// </summary>
    private static Func<HttpContext, CancellationToken, Task<IResult>> CreateBindAsyncHandler<
        TEndpoint,
        TRequest,
        TResponse
    >()
        where TEndpoint : class, IEndpoint<TRequest, TResponse>
    {
        return async (context, cancel) =>
        {
            var bindMethod = typeof(TRequest).GetMethod(
                "BindAsync",
                BindingFlags.Public | BindingFlags.Static,
                [typeof(HttpContext), typeof(ParameterInfo)]
            )!;

            var result = bindMethod.Invoke(null, [context, null!]);
            var request = await (ValueTask<TRequest>)result!;

            var endpoint = context.RequestServices.GetRequiredService<TEndpoint>();
            var sender = new ResponseSender<TResponse>();
            await endpoint.HandleAsync(sender, request, cancel);
            return sender.Result ?? Results.Ok();
        };
    }

    /// <summary>
    /// IEndpoint&lt;TResponse&gt;: no request parameters at all.
    /// </summary>
    private static Func<HttpContext, CancellationToken, Task<IResult>> CreateNoRequestHandler<
        TEndpoint,
        TResponse
    >()
        where TEndpoint : class, IEndpoint<TResponse>
    {
        return async (context, cancel) =>
        {
            var endpoint = context.RequestServices.GetRequiredService<TEndpoint>();
            var sender = new ResponseSender<TResponse>();
            await endpoint.HandleAsync(sender, cancel);
            return sender.Result ?? Results.Ok();
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
