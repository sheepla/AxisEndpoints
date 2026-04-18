using System.ComponentModel.DataAnnotations;

namespace AxisEndpoints.Example.Features.Users.Create;

/// <summary>
/// Demonstrates DataAnnotations validation.
/// All attributes are checked automatically before HandleAsync is called.
/// </summary>
public class CreateUserRequest
{
    [Required]
    [MaxLength(100)]
    public required string Name { get; init; }

    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    /// <summary>Assigned role. Defaults to "User" if omitted.</summary>
    [MaxLength(50)]
    public string Role { get; init; } = "User";
}
