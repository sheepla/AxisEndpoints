using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace AxisEndpoints.Example.Features.Users.Delete;

public class DeleteUserRequest
{
    [FromRoute]
    [Range(1, int.MaxValue, ErrorMessage = "Id must be a positive integer.")]
    public required int Id { get; init; }
}
