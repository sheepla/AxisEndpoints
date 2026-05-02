using AxisEndpoints;

namespace AxisEndpoints.Tutorial.Features.Hello;

public record HelloRequest
{
    public required string Name { get; set; } = string.Empty;
}

public record HelloResponse
{
    public required string Message { get; set; } = string.Empty;
}

public class HelloEndpoint(ILogger<HelloEndpoint> logger)
    : IEndpoint<HelloRequest, Response<HelloResponse>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config
            .Get("/hello")
            .Summary("Hello")
            .Description("This endpoint takes a name as input and returns a greeting message.");
    }

    public Task<Response<HelloResponse>> HandleAsync(HelloRequest request, CancellationToken cancel)
    {
        logger.LogInformation("Received request to /hello with name: {Name}", request.Name);

        return Task.FromResult(
            new Response<HelloResponse>
            {
                Body = new HelloResponse { Message = $"Hello, {request.Name}!" },
            }
        );
    }
}
