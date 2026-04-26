using FamilyTreeApiV2.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTreeApiV2.Features.Residences;

[ApiController]
[Route("boards/{boardId:guid}/residences")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class ResidencesController(
    ResidencesHandler handler,
    IValidator<CreateResidenceRequest> createResidenceValidator,
    IValidator<UpdateResidenceRequest> updateResidenceValidator)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<ResidenceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(Guid boardId)
    {
        var userId = User.GetUserId();
        var result = await handler.GetAllAsync(boardId, userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType<ResidenceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid boardId, [FromBody] CreateResidenceRequest request)
    {
        var validation = await createResidenceValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));

        var userId = User.GetUserId();
        var result = await handler.CreateAsync(boardId, request, userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPut("{residenceId:guid}")]
    [ProducesResponseType<ResidenceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid boardId, Guid residenceId, [FromBody] UpdateResidenceRequest request)
    {
        var validation = await updateResidenceValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));

        var userId = User.GetUserId();
        var result = await handler.UpdateAsync(boardId, residenceId, request, userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : Ok(result.Value);
    }

    [HttpDelete("{residenceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid boardId, Guid residenceId)
    {
        var userId = User.GetUserId();
        var result = await handler.DeleteAsync(boardId, residenceId, userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : NoContent();
    }
}
