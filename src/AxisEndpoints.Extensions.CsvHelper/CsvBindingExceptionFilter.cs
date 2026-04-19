using Microsoft.AspNetCore.Http;

namespace AxisEndpoints.Extensions.CsvHelper;

/// <summary>
/// An endpoint filter that catches <see cref="CsvBindingException"/> thrown during CSV
/// request binding and converts it to an RFC 9457 <c>ValidationProblem</c> response,
/// consistent with the error shape produced by AxisEndpoints' built-in
/// DataAnnotations validation filter.
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
        try
        {
            return await next(context);
        }
        catch (CsvBindingException ex)
        {
            return TypedResults.ValidationProblem(ex.Errors);
        }
    }
}
