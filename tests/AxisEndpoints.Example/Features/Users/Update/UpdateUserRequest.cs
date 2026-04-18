using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace AxisEndpoints.Example.Features.Users.Update;

/// <summary>
/// Demonstrates the Minimal API BindAsync convention on a request type.
/// AxisEndpoints detects the static BindAsync method and delegates binding to it,
/// enabling multipart/form-data requests that mix file uploads with text fields.
/// </summary>
public class UpdateUserRequest
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }

    /// <summary>Optional profile picture. Null when not included in the request.</summary>
    public IFormFile? Avatar { get; init; }

    /// <summary>
    /// Called by AxisEndpoints instead of the default property-based binder
    /// when this method is present (Minimal API convention).
    /// </summary>
    public static async ValueTask<UpdateUserRequest> BindAsync(HttpContext context, ParameterInfo _)
    {
        // Route value is available via HttpContext even in BindAsync.
        var idRaw =
            context.GetRouteValue("id")?.ToString()
            ?? throw new BadHttpRequestException("Route parameter 'id' is missing.");

        if (!int.TryParse(idRaw, out var id) || id < 1)
        {
            throw new BadHttpRequestException("Route parameter 'id' must be a positive integer.");
        }

        var form = await context.Request.ReadFormAsync();

        var name =
            form["name"].FirstOrDefault()
            ?? throw new BadHttpRequestException("Form field 'name' is required.");

        var email =
            form["email"].FirstOrDefault()
            ?? throw new BadHttpRequestException("Form field 'email' is required.");

        var avatar = form.Files["avatar"];

        return new UpdateUserRequest
        {
            Id = id,
            Name = name,
            Email = email,
            Avatar = avatar,
        };
    }
}
