using AxisEndpoints;
using AxisEndpoints.Extensions.CsvHelper;
using CsvHelper.Configuration.Attributes;

namespace AxisEndpoints.Example.Features.Users.ExportToCsv;

// ---------------------------------------------------------------------------
// Row model
// ---------------------------------------------------------------------------

/// <summary>
/// Represents a single user row written to an exported CSV file.
/// </summary>
public sealed class UserExportRow
{
    [Name("id")]
    public int Id { get; init; }

    [Name("name")]
    public string Name { get; init; } = string.Empty;

    [Name("email")]
    public string Email { get; init; } = string.Empty;

    [Name("role")]
    public string Role { get; init; } = string.Empty;
}

// ---------------------------------------------------------------------------
// Endpoint
// ---------------------------------------------------------------------------

/// <summary>
/// Demonstrates CSV export via <see cref="CsvResponse{TRow}"/>.
///   - Returns IAsyncEnumerable&lt;UserExportRow&gt; streamed directly to the response
///   - No full dataset buffering — suitable for large result sets
/// </summary>
public sealed class ExportToCsvEndpoint : IEndpoint<CsvResponse<UserExportRow>>
{
    public void Configure(IEndpointConfiguration config)
    {
        config
            .Get("/users/export")
            .Group<UsersEndpointGroup>()
            .Summary("Export users as CSV")
            .Description("Streams all users as a downloadable CSV file.");
    }

    public Task HandleAsync(
        IResponseSender<CsvResponse<UserExportRow>> sender,
        CancellationToken cancel
    )
    {
        var rows = FetchUsersAsync(cancel);

        return sender.SendAsync(CsvResponse.From(rows, fileName: "users.csv"), cancel);
    }

    private static async IAsyncEnumerable<UserExportRow> FetchUsersAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancel
    )
    {
        // Simulates async database enumeration. A real implementation would
        // yield rows from EF Core's AsAsyncEnumerable() or Dapper's QueryUnbufferedAsync().
        var seed = new[]
        {
            new UserExportRow
            {
                Id = 1,
                Name = "Alice",
                Email = "alice@example.com",
                Role = "Admin",
            },
            new UserExportRow
            {
                Id = 2,
                Name = "Bob",
                Email = "bob@example.com",
                Role = "User",
            },
        };

        foreach (var row in seed)
        {
            cancel.ThrowIfCancellationRequested();
            yield return row;
            await Task.Yield();
        }
    }
}
