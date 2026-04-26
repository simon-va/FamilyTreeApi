using FamilyTreeApiV2.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTreeApiV2.Features.Relations;

[ApiController]
[Route("boards/{boardId:guid}/relations")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class RelationsController(
    RelationsHandler handler,
    IValidator<CreateRelationRequest> createRelationValidator,
    IValidator<UpdateRelationRequest> updateRelationValidator)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<RelationResponse>>(StatusCodes.Status200OK)]
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
    [ProducesResponseType<RelationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid boardId, [FromBody] CreateRelationRequest request)
    {
        var validation = await createRelationValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));

        var userId = User.GetUserId();
        var result = await handler.CreateAsync(boardId, request, userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPut("{relationId:guid}")]
    [ProducesResponseType<RelationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid boardId, Guid relationId, [FromBody] UpdateRelationRequest request)
    {
        var validation = await updateRelationValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));

        var userId = User.GetUserId();
        var result = await handler.UpdateAsync(boardId, relationId, request, userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : Ok(result.Value);
    }

    [HttpDelete("{relationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid boardId, Guid relationId)
    {
        var userId = User.GetUserId();
        var result = await handler.DeleteAsync(boardId, relationId, userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : NoContent();
    }
}
