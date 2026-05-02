using System.Net;
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

public class HelloEndpoint(ILogger<HelloEndpoint> logger) : IEndpoint<HelloRequest, IResult>
{
    public void Configure(IEndpointConfiguration config)
    {
        config
            .Get("/hello")
            .ProducesSuccess<HelloResponse>()
            .ProducesError(HttpStatusCode.BadRequest)
            .Summary("Hello")
            .Description("This endpoint takes a name as input and returns a greeting message.");
    }

    public Task<IResult> HandleAsync(HelloRequest request, CancellationToken cancel)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            logger.LogWarning("Rejected request to /hello because the name was missing.");
            return Task.FromResult(
                Results.Problem(
                    title: "Name is required",
                    detail: "Provide a non-empty name query parameter.",
                    statusCode: StatusCodes.Status400BadRequest
                )
            );
        }

        logger.LogInformation("Received request to /hello with name: {Name}", request.Name);

        return Task.FromResult(
            Results.Json(new HelloResponse
            {
                Message = $"Hello, {request.Name}!",
            })
        );
    }
}
