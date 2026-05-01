using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace AxisEndpoints.Example.Features.Users.List;

/// <summary>
/// Demonstrates [FromQuery] binding with multiple parameters and DataAnnotations on query values.
/// Page and PageSize are nullable so that [AsParameters] binding correctly distinguishes
/// "not provided" (null) from an explicit value — property initializers on value types
/// are ignored by [AsParameters], which would otherwise default to 0.
/// </summary>
public class ListUsersRequest
{
    [FromQuery]
    [Range(1, int.MaxValue, ErrorMessage = "Page must be 1 or greater.")]
    public int? Page { get; init; }

    [FromQuery]
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int? PageSize { get; init; }

    /// <summary>Optional role filter. Returns all roles when omitted.</summary>
    [FromQuery]
    [MaxLength(50)]
    public string? Role { get; init; }
}
