namespace AxisEndpoints.Example.Features.Users;

/// <summary>
/// Shared user representation returned by FindById, List, Create, and Update.
/// </summary>
public class UserResponse
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string Role { get; init; }
}
