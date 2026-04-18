using System.Reflection;
using Microsoft.AspNetCore.Authorization;
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
        var endpointTypes = assemblies.SelectMany(a => a.GetTypes()).Where(IsEndpointType);

        foreach (var type in endpointTypes)
        {
            services.AddScoped(type);
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
            ApplyMetadata(routeHandlerBuilder, config, options);
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
        // Skip the constructor entirely — Configure() has no DI dependencies.
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
        // POST/PUT/PATCH bind TRequest from the body.
        // GET/DELETE/HEAD have no body, so TRequest is bound from route values and query string.
        var usesBody =
            config.Method
            is HttpEndpointMethod.Post
                or HttpEndpointMethod.Put
                or HttpEndpointMethod.Patch;

        var handler = usesBody
            ? MakeHandler(endpointType, requestType, responseType)
            : MakeHandlerFromContext(endpointType, requestType, responseType);

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
            // The filter is constructed inline rather than resolved from DI because it is a
            // generic type parameterized on TRequest, which cannot be registered in DI generically.
            var filterType = typeof(DataAnnotationsValidationFilter<>).MakeGenericType(requestType);
            var filter = (IEndpointFilter)Activator.CreateInstance(filterType)!;
            routeHandlerBuilder.AddEndpointFilter(filter);
        }

        return routeHandlerBuilder;
    }

    private static RouteHandlerBuilder MapResponseOnlyRoute(
        IEndpointRouteBuilder builder,
        Type endpointType,
        EndpointConfiguration config,
        Type responseType
    )
    {
        var handler = MakeHandlerNoRequest(endpointType, responseType);
        return config.Method switch
        {
            HttpEndpointMethod.Get => builder.MapGet(config.Route, handler),
            HttpEndpointMethod.Post => builder.MapPost(config.Route, handler),
            HttpEndpointMethod.Put => builder.MapPut(config.Route, handler),
            HttpEndpointMethod.Patch => builder.MapPatch(config.Route, handler),
            HttpEndpointMethod.Delete => builder.MapDelete(config.Route, handler),
            HttpEndpointMethod.Head => builder.MapMethods(config.Route, ["HEAD"], handler),
            _ => throw new InvalidOperationException($"Unsupported HTTP method: {config.Method}"),
        };
    }

    private static Delegate MakeHandler(Type endpointType, Type requestType, Type responseType)
    {
        var method = typeof(EndpointRegistry)
            .GetMethod(nameof(CreateHandler), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(endpointType, requestType, responseType);

        return (Delegate)method.Invoke(null, [])!;
    }

    private static Delegate MakeHandlerFromContext(
        Type endpointType,
        Type requestType,
        Type responseType
    )
    {
        var method = typeof(EndpointRegistry)
            .GetMethod(
                nameof(CreateHandlerFromContext),
                BindingFlags.NonPublic | BindingFlags.Static
            )!
            .MakeGenericMethod(endpointType, requestType, responseType);

        return (Delegate)method.Invoke(null, [])!;
    }

    private static Delegate MakeHandlerNoRequest(Type endpointType, Type responseType)
    {
        var method = typeof(EndpointRegistry)
            .GetMethod(
                nameof(CreateHandlerNoRequest),
                BindingFlags.NonPublic | BindingFlags.Static
            )!
            .MakeGenericMethod(endpointType, responseType);

        return (Delegate)method.Invoke(null, [])!;
    }

    // POST/PUT/PATCH: TRequest is automatically bound from the request body.
    private static Func<TRequest, HttpContext, CancellationToken, Task> CreateHandler<
        TEndpoint,
        TRequest,
        TResponse
    >()
        where TEndpoint : class, IEndpoint<TRequest, TResponse>
    {
        return async (TRequest request, HttpContext context, CancellationToken cancel) =>
        {
            var endpoint = context.RequestServices.GetRequiredService<TEndpoint>();
            var sender = new ResponseSender<TResponse>(context);
            await endpoint.HandleAsync(sender, request, cancel);
        };
    }

    // GET/DELETE/HEAD: TRequest is bound from route values and query string.
    private static Func<HttpContext, CancellationToken, Task> CreateHandlerFromContext<
        TEndpoint,
        TRequest,
        TResponse
    >()
        where TEndpoint : class, IEndpoint<TRequest, TResponse>
    {
        return async (HttpContext context, CancellationToken cancel) =>
        {
            var request = await BindRequestAsync<TRequest>(context, cancel);
            var endpoint = context.RequestServices.GetRequiredService<TEndpoint>();
            var sender = new ResponseSender<TResponse>(context);
            await endpoint.HandleAsync(sender, request, cancel);
        };
    }

    private static Func<HttpContext, CancellationToken, Task> CreateHandlerNoRequest<
        TEndpoint,
        TResponse
    >()
        where TEndpoint : class, IEndpoint<TResponse>
    {
        return async (HttpContext context, CancellationToken cancel) =>
        {
            var endpoint = context.RequestServices.GetRequiredService<TEndpoint>();
            var sender = new ResponseSender<TResponse>(context);
            await endpoint.HandleAsync(sender, cancel);
        };
    }

    /// <summary>
    /// Binds TRequest from route values and query string.
    /// Delegates to the type's static BindAsync method if present (Minimal API convention),
    /// otherwise falls back to setting writable properties by name.
    /// </summary>
    private static async ValueTask<TRequest> BindRequestAsync<TRequest>(
        HttpContext context,
        CancellationToken cancel
    )
    {
        var bindMethod = typeof(TRequest).GetMethod(
            "BindAsync",
            BindingFlags.Public | BindingFlags.Static,
            [typeof(HttpContext), typeof(ParameterInfo)]
        );

        if (bindMethod is not null)
        {
            var result = bindMethod.Invoke(null, [context, null!]);
            return await (ValueTask<TRequest>)result!;
        }

        // Fallback: populate writable properties from route values then query string.
        var instance = Activator.CreateInstance<TRequest>()!;
        var properties = typeof(TRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        foreach (var prop in properties)
        {
            var rawValue =
                context.GetRouteValue(prop.Name)?.ToString()
                ?? context.Request.Query[prop.Name].FirstOrDefault();

            if (rawValue is null)
            {
                continue;
            }

            var converted = Convert.ChangeType(rawValue, prop.PropertyType);
            prop.SetValue(instance, converted);
        }

        return instance;
    }

    private static void ApplyMetadata(
        RouteHandlerBuilder routeBuilder,
        EndpointConfiguration config,
        AxisEndpointsOptions options
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

        // Filters are resolved from DI at request time via AddEndpointFilter(Type).
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
    }
}
