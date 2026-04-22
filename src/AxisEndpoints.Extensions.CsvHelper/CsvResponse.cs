using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;

namespace AxisEndpoints.Extensions.CsvHelper;

/// <summary>
/// An <see cref="IResult"/> that serializes a sequence of <typeparamref name="TRow"/> records
/// as a CSV response, writing directly to the response stream without buffering the entire
/// dataset in memory.
///
/// Because this type implements <see cref="IResult"/>, the AxisEndpoints framework
/// delegates execution directly to <see cref="ExecuteAsync"/> rather than wrapping it in
/// <c>Results.Json</c>.
///
/// Return an instance directly from <c>HandleAsync</c> via the static factory on <see cref="CsvResponse"/>:
/// <code>
/// return Task.FromResult(CsvResponse.From(rows));
/// return Task.FromResult(CsvResponse.From(rows, classMap: new UserRowMap()));
/// </code>
/// </summary>
/// <typeparam name="TRow">The strongly-typed row model.</typeparam>
public sealed class CsvResponse<TRow> : IResult
{
    private readonly IAsyncEnumerable<TRow> _rows;
    private readonly CsvConfiguration _configuration;
    private readonly ClassMap? _classMap;
    private readonly string _fileName;
    private readonly HttpStatusCode _statusCode;

    internal CsvResponse(
        IAsyncEnumerable<TRow> rows,
        CsvConfiguration configuration,
        ClassMap? classMap,
        string fileName,
        HttpStatusCode statusCode
    )
    {
        _rows = rows;
        _configuration = configuration;
        _classMap = classMap;
        _fileName = fileName;
        _statusCode = statusCode;
    }

    /// <summary>
    /// Writes the CSV rows to the HTTP response stream.
    /// Sets <c>Content-Type: text/csv</c> and <c>Content-Disposition: attachment</c> headers,
    /// then streams rows via CsvHelper without loading the full dataset into memory.
    /// </summary>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var response = httpContext.Response;

        response.StatusCode = (int)_statusCode;
        response.ContentType = "text/csv; charset=utf-8";
        response.Headers["Content-Disposition"] = $"attachment; filename=\"{_fileName}\"";

        await using var writer = new StreamWriter(response.Body, leaveOpen: true);
        await using var csv = new CsvWriter(writer, _configuration);

        if (_classMap is not null)
        {
            csv.Context.RegisterClassMap(_classMap);
        }

        csv.WriteHeader<TRow>();
        await csv.NextRecordAsync();

        await foreach (var row in _rows)
        {
            csv.WriteRecord(row);
            await csv.NextRecordAsync();
        }

        await writer.FlushAsync();
    }
}

/// <summary>
/// Static factory for creating <see cref="CsvResponse{TRow}"/> instances.
/// </summary>
public static class CsvResponse
{
    /// <summary>
    /// Creates a CSV response from an <see cref="IAsyncEnumerable{TRow}"/> sequence.
    /// Rows are written to the response stream as they are produced, suitable for large
    /// datasets fetched from a database or external source.
    /// </summary>
    /// <param name="rows">The row sequence to serialize.</param>
    /// <param name="classMap">
    /// Optional CsvHelper <see cref="ClassMap"/> for column mapping and formatting.
    /// When <c>null</c>, CsvHelper's convention-based mapping and any attributes on
    /// <typeparamref name="TRow"/> are used.
    /// </param>
    /// <param name="fileName">
    /// The filename sent in the <c>Content-Disposition</c> header. Defaults to <c>export.csv</c>.
    /// </param>
    /// <param name="statusCode">HTTP status code. Defaults to 200 OK.</param>
    /// <param name="configuration">
    /// CsvHelper write configuration. Defaults to <see cref="CultureInfo.InvariantCulture"/>.
    /// </param>
    public static CsvResponse<TRow> From<TRow>(
        IAsyncEnumerable<TRow> rows,
        ClassMap? classMap = null,
        string fileName = "export.csv",
        HttpStatusCode statusCode = HttpStatusCode.OK,
        CsvConfiguration? configuration = null
    ) =>
        new(
            rows,
            configuration ?? new CsvConfiguration(CultureInfo.InvariantCulture),
            classMap,
            fileName,
            statusCode
        );

    /// <summary>
    /// Creates a CSV response from a synchronous <see cref="IEnumerable{TRow}"/> sequence.
    /// The sequence is adapted to <see cref="IAsyncEnumerable{TRow}"/> internally.
    /// Prefer the <see cref="IAsyncEnumerable{TRow}"/> overload for database-backed sequences.
    /// </summary>
    /// <param name="rows">The row sequence to serialize.</param>
    /// <param name="classMap">Optional CsvHelper <see cref="ClassMap"/> for column mapping.</param>
    /// <param name="fileName">Filename for the <c>Content-Disposition</c> header.</param>
    /// <param name="statusCode">HTTP status code. Defaults to 200 OK.</param>
    /// <param name="configuration">CsvHelper write configuration.</param>
    public static CsvResponse<TRow> From<TRow>(
        IEnumerable<TRow> rows,
        ClassMap? classMap = null,
        string fileName = "export.csv",
        HttpStatusCode statusCode = HttpStatusCode.OK,
        CsvConfiguration? configuration = null
    ) => From(ToAsyncEnumerable(rows), classMap, fileName, statusCode, configuration);

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        IEnumerable<T> source,
        [EnumeratorCancellation] CancellationToken cancel = default
    )
    {
        foreach (var item in source)
        {
            cancel.ThrowIfCancellationRequested();
            yield return item;
        }
    }
}
