namespace AxisEndpoints.Extensions.CsvHelper;

/// <summary>
/// Non-generic interface exposing CSV binding validation errors. Implemented by
/// <see cref="CsvRequest{TRow}"/> so that <see cref="CsvBindingExceptionFilter"/>
/// can inspect binding errors without knowing the concrete generic type.
/// </summary>
public interface ICsvBindingErrors
{
    /// <summary>
    /// Validation errors collected during binding, keyed by "row {n}: {memberName}".
    /// Empty when all rows pass validation.
    /// </summary>
    IReadOnlyDictionary<string, string[]> BindingErrors { get; }
}
