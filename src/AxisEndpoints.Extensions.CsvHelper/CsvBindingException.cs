namespace AxisEndpoints.Extensions.CsvHelper;

/// <summary>
/// Thrown by <see cref="CsvRequest{TRow}"/> when one or more rows fail DataAnnotations
/// validation during binding. The <see cref="Errors"/> dictionary follows the same shape
/// as <c>TypedResults.ValidationProblem</c> so it can be forwarded directly to the client.
/// </summary>
public sealed class CsvBindingException : Exception
{
    /// <summary>
    /// Validation errors keyed by "row {n}: {memberName}".
    /// Values are the error messages for that member on that row.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public CsvBindingException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more CSV rows failed validation.")
    {
        Errors = errors;
    }
}
