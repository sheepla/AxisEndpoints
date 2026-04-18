using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace AxisEndpoints.Example.Features.Users.FindById;

/// <summary>
/// Demonstrates [FromRoute] binding for a GET endpoint.
/// [Range] shows DataAnnotations working on route-bound parameters.
/// </summary>
public class FindByIdRequest
{
    [FromRoute]
    [Range(1, int.MaxValue, ErrorMessage = "Id must be a positive integer.")]
    public required int Id { get; init; }
}
