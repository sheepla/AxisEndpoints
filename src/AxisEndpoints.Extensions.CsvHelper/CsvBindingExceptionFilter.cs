using Microsoft.AspNetCore.Http;

namespace AxisEndpoints.Extensions.CsvHelper;

/// <summary>
/// An endpoint filter that inspects <see cref="CsvRequest{TRow}.BindingErrors"/> collected
/// during CSV request binding and converts them to an RFC 9457 <c>ValidationProblem</c>
/// response, consistent with the error shape produced by AxisEndpoints' built-in
/// DataAnnotations validation filter.
///
/// Because <c>BindAsync</c> executes before the endpoint filter pipeline, the previous
/// approach of catching <see cref="CsvBindingException"/> in a try/catch could never work.
/// This filter instead checks the bound request argument for deferred errors.
///
/// Register this filter on endpoints that accept a <see cref="CsvRequest{TRow}"/> parameter:
/// <code>
/// config.Post("/import").AddFilter&lt;CsvBindingExceptionFilter&gt;();
/// </code>
/// </summary>
public sealed class CsvBindingExceptionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        foreach (var argument in context.Arguments)
        {
            if (argument is ICsvBindingErrors { BindingErrors: { Count: > 0 } errors })
            {
                return TypedResults.ValidationProblem(errors);
            }
        }

        return await next(context);
    }
}
