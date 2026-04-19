using System.ComponentModel.DataAnnotations;
using AxisEndpoints;
using AxisEndpoints.Extensions.CsvHelper;
using CsvHelper.Configuration.Attributes;
using Microsoft.AspNetCore.Http;

namespace AxisEndpoints.Example.Features.Users.ImportFromCsv;

// ---------------------------------------------------------------------------
// Row model
// ---------------------------------------------------------------------------

/// <summary>
/// Represents a single user row read from an imported CSV file.
/// CsvHelper attributes handle column name mapping; DataAnnotations handle validation.
/// </summary>
public sealed class UserImportRow
{
    [Name("name")]
    [Required]
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [Name("email")]
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Name("role")]
    [Required]
    public string Role { get; init; } = string.Empty;
}

// ---------------------------------------------------------------------------
// Request type
// ---------------------------------------------------------------------------

/// <summary>
/// CSV import request. Delegates binding to <see cref="CsvRequest{TRow}.BindCsvAsync{TDerived}"/>.
/// Minimal API requires BindAsync to be a non-generic static method on the concrete type,
/// so it cannot be declared on the base class.
/// </summary>
public sealed class ImportFromCsvRequest : CsvRequest<UserImportRow>
{
    public static ValueTask<ImportFromCsvRequest> BindAsync(HttpContext context) =>
        BindCsvAsync<ImportFromCsvRequest>(context);
}

// ---------------------------------------------------------------------------
// Endpoint
// ---------------------------------------------------------------------------

/// <summary>
/// Demonstrates CSV import via <see cref="CsvRequest{TRow}"/>.
///   - Accepts text/csv or multipart/form-data
///   - DataAnnotations on <see cref="UserImportRow"/> are validated per-row during binding
///   - <see cref="CsvBindingExceptionFilter"/> converts row-level errors to ValidationProblem
/// </summary>
public sealed class ImportFromCsvEndpoint(ILogger<ImportFromCsvEndpoint> logger)
    : IEndpoint<ImportFromCsvRequest, EmptyResponse>
{
    public void Configure(IEndpointConfiguration config)
    {
        config
            .Post("/users/import")
            .Group<UsersEndpointGroup>()
            .AddFilter<CsvBindingExceptionFilter>()
            .Summary("Import users from CSV")
            .Description(
                "Accepts a CSV file with columns: name, email, role. "
                    + "Validates each row and returns a ValidationProblem on failure."
            );
    }

    public Task HandleAsync(
        IResponseSender<EmptyResponse> sender,
        ImportFromCsvRequest request,
        CancellationToken cancel
    )
    {
        // request.Rows is IReadOnlyList<UserImportRow> — all rows have passed validation.
        // A real implementation would persist them via a repository.
        var rows = request.Rows;

        logger.LogInformation("Importing {Count} users from CSV", rows.Count);
        foreach (var row in rows)
        {
            logger.LogInformation(
                "Processing user: {Name} <{Email}> ({Role})",
                row.Name,
                row.Email,
                row.Role
            );
        }

        return sender
            .StatusCode(System.Net.HttpStatusCode.NoContent)
            .SendAsync(EmptyResponse.Instance, cancel);
    }
}
