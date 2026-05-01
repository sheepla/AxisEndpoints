using System.ComponentModel.DataAnnotations;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;

namespace AxisEndpoints.Extensions.CsvHelper;

/// <summary>
/// Base class for CSV request types. Reads <c>text/csv</c> or <c>multipart/form-data</c>
/// request bodies using CsvHelper and exposes the parsed rows as
/// <see cref="IReadOnlyList{TRow}"/>.
///
/// Minimal API requires <c>BindAsync</c> to be declared as a non-generic static method
/// on the concrete parameter type itself. Because of this constraint, the base class
/// cannot provide <c>BindAsync</c> directly. Instead, declare it in the derived class
/// and delegate to <see cref="BindCsvAsync{TDerived}"/>:
/// <code>
/// public sealed class ImportUsersRequest : CsvRequest&lt;UserRow&gt;
/// {
///     public static ValueTask&lt;ImportUsersRequest&gt; BindAsync(HttpContext context)
///         =&gt; BindCsvAsync&lt;ImportUsersRequest&gt;(context);
/// }
/// </code>
///
/// Override <see cref="CreateConfiguration"/> or <see cref="CreateClassMap"/> on the
/// derived class to customise CsvHelper behaviour without touching the binding logic.
///
/// DataAnnotations placed on <typeparamref name="TRow"/> are validated per-row during binding.
/// Errors are collected across all rows and stored in <see cref="BindingErrors"/> so that
/// <see cref="CsvBindingExceptionFilter"/> can inspect them inside the endpoint filter pipeline.
/// </summary>
/// <typeparam name="TRow">The strongly-typed row model.</typeparam>
public abstract class CsvRequest<TRow> : ICsvBindingErrors
{
    /// <summary>The rows parsed from the CSV body. Empty when the body contained no data rows.</summary>
    public IReadOnlyList<TRow> Rows { get; private set; } = [];

    /// <summary>
    /// Validation errors collected during binding, keyed by "row {n}: {memberName}".
    /// Empty when all rows pass validation. Inspected by <see cref="CsvBindingExceptionFilter"/>
    /// to return a <c>ValidationProblem</c> response before the handler is invoked.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> BindingErrors { get; private set; } =
        new Dictionary<string, string[]>();

    // -------------------------------------------------------------------------
    // Binding helper — call this from the derived class's BindAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Core CSV binding implementation. Declare a non-generic <c>BindAsync</c> on the
    /// derived class and delegate here so Minimal API can resolve the correct signature.
    /// </summary>
    /// <typeparam name="TDerived">The concrete request type being bound.</typeparam>
    protected static async ValueTask<TDerived> BindCsvAsync<TDerived>(HttpContext context)
        where TDerived : CsvRequest<TRow>, new()
    {
        var instance = new TDerived();
        var csvConfig = instance.CreateConfiguration();
        var classMap = instance.CreateClassMap();

        await using var stream = await ResolveBodyStreamAsync(context);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, csvConfig);

        if (classMap is not null)
        {
            csv.Context.RegisterClassMap(classMap);
        }

        var rows = new List<TRow>();
        var errors = new Dictionary<string, string[]>();

        await foreach (var row in csv.GetRecordsAsync<TRow>())
        {
            // Parser is non-null after the first read; Row is the 1-based physical line number.
            var rowNumber = csv.Context.Parser?.Row ?? 0;
            ValidateRow(row, rowNumber, errors);
            rows.Add(row);
        }

        if (errors.Count > 0)
        {
            instance.BindingErrors = errors;
        }

        instance.Rows = rows;
        return instance;
    }

    // -------------------------------------------------------------------------
    // Extension points
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the CsvHelper configuration used when reading the request body.
    /// Override to customise delimiter, encoding, header behaviour, etc.
    /// The default uses <see cref="System.Globalization.CultureInfo.InvariantCulture"/>
    /// and expects a header row.
    /// </summary>
    protected virtual CsvConfiguration CreateConfiguration() =>
        new(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// Returns a <see cref="ClassMap"/> registered with the <see cref="CsvReader"/> before
    /// reading begins. Return <c>null</c> (the default) to rely on CsvHelper's convention-based
    /// mapping or on attributes applied directly to <typeparamref name="TRow"/>.
    /// </summary>
    protected virtual ClassMap? CreateClassMap() => null;

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Resolves the raw CSV stream from the request.
    /// Supports both <c>text/csv</c> direct body and <c>multipart/form-data</c> file upload
    /// (uses the first file found in the form).
    /// </summary>
    private static async Task<Stream> ResolveBodyStreamAsync(HttpContext context)
    {
        var contentType = context.Request.ContentType ?? string.Empty;

        if (contentType.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            var form = await context.Request.ReadFormAsync();
            var file =
                form.Files.FirstOrDefault()
                ?? throw new BadHttpRequestException(
                    "Expected a CSV file in the multipart/form-data request."
                );
            return file.OpenReadStream();
        }

        // Assume the body itself is the CSV content (text/csv or similar).
        // Return the body stream wrapped in a MemoryStream so callers can dispose it
        // without closing the original request body.
        var ms = new MemoryStream();
        await context.Request.Body.CopyToAsync(ms);
        ms.Position = 0;
        return ms;
    }

    /// <summary>
    /// Validates a single row using DataAnnotations and appends any errors to the
    /// <paramref name="errors"/> dictionary keyed by <c>"row {rowNumber}: {memberName}"</c>.
    /// </summary>
    private static void ValidateRow(
        TRow row,
        int rowNumber,
        Dictionary<string, string[]> errors
    )
    {
        if (row is null)
        {
            return;
        }

        var context = new ValidationContext(row);
        var results = new List<ValidationResult>();

        if (Validator.TryValidateObject(row, context, results, validateAllProperties: true))
        {
            return;
        }

        foreach (var result in results)
        {
            var memberName = result.MemberNames.FirstOrDefault() ?? "(unknown)";
            var key = $"row {rowNumber}: {memberName}";
            errors[key] = [result.ErrorMessage ?? "Validation failed."];
        }
    }
}
