using System.ComponentModel.DataAnnotations;
using AxisEndpoints;
using AxisEndpoints.Extensions.CsvHelper;
using CsvHelper.Configuration.Attributes;
using Microsoft.AspNetCore.Http;

namespace AxisEndpoints.Example.Features.Users.ImportFromCsv;

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

/// <summary>
/// Demonstrates CSV import via <see cref="CsvRequest{TRow}"/>.
///   - Accepts text/csv or multipart/form-data
///   - DataAnnotations on UserImportRow are validated per-row during binding
///   - CsvBindingExceptionFilter converts row-level errors to ValidationProblem
/// </summary>
public sealed class ImportFromCsvEndpoint : IEndpoint<ImportFromCsvRequest, Response<EmptyResponse>>
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

    public Task<Response<EmptyResponse>> HandleAsync(
        ImportFromCsvRequest request,
        CancellationToken cancel
    )
    {
        // request.Rows is IReadOnlyList<UserImportRow> — all rows have passed validation.
        // A real implementation would persist them via a repository.
        _ = request.Rows;

        return Task.FromResult(Response.NoContent);
    }
}
