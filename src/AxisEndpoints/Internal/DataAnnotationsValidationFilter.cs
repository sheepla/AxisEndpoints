using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AxisEndpoints.Internal;

/// <summary>
/// An endpoint filter that validates the first argument of type <typeparamref name="TRequest"/>
/// using DataAnnotations. Returns <see cref="TypedResults.ValidationProblem"/> on failure,
/// following RFC 9457 / ASP.NET Core's standard problem details shape.
/// </summary>
internal sealed class DataAnnotationsValidationFilter<TRequest> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();

        // No request argument found — nothing to validate, pass through.
        if (request is null)
        {
            return await next(context);
        }

        var validationContext = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();

        if (
            Validator.TryValidateObject(
                request,
                validationContext,
                validationResults,
                validateAllProperties: true
            )
        )
        {
            return await next(context);
        }

        // Group messages by member name, falling back to an empty key for object-level errors.
        var errors = validationResults
            .SelectMany(result =>
                result
                    .MemberNames.DefaultIfEmpty(string.Empty)
                    .Select(member => (member, result.ErrorMessage ?? "Validation failed."))
            )
            .GroupBy(x => x.member, x => x.Item2)
            .ToDictionary(g => g.Key, g => g.ToArray());

        return TypedResults.ValidationProblem(errors);
    }
}
