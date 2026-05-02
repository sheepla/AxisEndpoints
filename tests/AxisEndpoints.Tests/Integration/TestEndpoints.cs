using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace AxisEndpoints.Tests.Integration;

public class HelloEndpoint : IEndpoint<Response<HelloResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Get("/hello").Tags("test");
    }

    public Task<Response<HelloResponse>> HandleAsync(CancellationToken cancel)
    {
        return Task.FromResult(
            new Response<HelloResponse> { Body = new HelloResponse("Hello, World!") }
        );
    }
}

public record HelloResponse(string Message);

public class EchoEndpoint : IEndpoint<EchoRequest, Response<EchoResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Post("/echo");
    }

    public Task<Response<EchoResponse>> HandleAsync(EchoRequest request, CancellationToken cancel)
    {
        return Task.FromResult(
            new Response<EchoResponse> { Body = new EchoResponse(request.Message) }
        );
    }
}

public record EchoRequest(string Message);

public record EchoResponse(string Echo);

public class ValidatedEndpoint : IEndpoint<ValidatedRequest, Response<EmptyResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Post("/validated");
    }

    public Task<Response<EmptyResponse>> HandleAsync(
        ValidatedRequest request,
        CancellationToken cancel
    )
    {
        return Task.FromResult(Response.Empty);
    }
}

public record ValidatedRequest
{
    [Required]
    [StringLength(10)]
    public string Name { get; init; } = "";
}

public class DeleteItemEndpoint : IEndpoint<DeleteItemRequest, Response<EmptyResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Delete("/items/{id}").ProducesSuccess<EmptyResponse>(HttpStatusCode.NoContent);
    }

    public Task<Response<EmptyResponse>> HandleAsync(
        DeleteItemRequest request,
        CancellationToken cancel
    )
    {
        return Task.FromResult(Response.NoContent);
    }
}

public record DeleteItemRequest([property: FromRoute] int Id);

public class CreatedHelloEndpoint : IEndpoint<Response<HelloResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Post("/hello-created").ProducesSuccess<HelloResponse>(HttpStatusCode.Created);
    }

    public Task<Response<HelloResponse>> HandleAsync(CancellationToken cancel)
    {
        return Task.FromResult(
            new Response<HelloResponse>
            {
                StatusCode = HttpStatusCode.Created,
                Body = new HelloResponse("Created!"),
            }
        );
    }
}

public class ApiGroup : IEndpointGroup
{
    public void Configure(IEndpointGroupConfiguration config)
    {
        config.Prefix("/api").Tags("api");
    }
}

public class GroupedEndpoint : IEndpoint<Response<HelloResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config.Get("/grouped").Group<ApiGroup>();
    }

    public Task<Response<HelloResponse>> HandleAsync(CancellationToken cancel)
    {
        return Task.FromResult(
            new Response<HelloResponse> { Body = new HelloResponse("Grouped!") }
        );
    }
}
