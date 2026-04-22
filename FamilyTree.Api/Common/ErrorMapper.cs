using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTreeApiV2.Common;

public static class ErrorMapper
{
    public static IActionResult ToActionResult(List<Error> errors, ControllerBase controller)
    {
        if (errors.Count == 0)
            return controller.StatusCode(500);

        if (errors.All(e => e.Type == ErrorType.Validation))
        {
            var problemDetails = new ValidationProblemDetails(
                errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray())
            );
            return controller.BadRequest(problemDetails);
        }

        var firstError = errors.First(e => e.Type != ErrorType.Validation);

        return firstError.Type switch
        {
            ErrorType.NotFound     => controller.NotFound(new { firstError.Code, firstError.Description }),
            ErrorType.Conflict     => controller.Conflict(new { firstError.Code, firstError.Description }),
            ErrorType.Unauthorized => controller.Unauthorized(),
            _                      => controller.StatusCode(500, new { firstError.Code, firstError.Description })
        };
    }
}
