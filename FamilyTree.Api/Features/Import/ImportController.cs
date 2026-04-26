using FamilyTreeApiV2.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTreeApiV2.Features.Import;

[ApiController]
[Route("import")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class ImportController(
    ImportHandler handler,
    IValidator<ImportRequest> importValidator)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ImportResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Import([FromBody] ImportRequest request)
    {
        var validation = await importValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));

        var userId = User.GetUserId();
        var result = await handler.ImportAsync(request, userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : StatusCode(StatusCodes.Status201Created, result.Value);
    }
}
