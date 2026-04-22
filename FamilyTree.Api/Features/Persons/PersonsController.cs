using System.Security.Claims;
using FamilyTreeApiV2.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTreeApiV2.Features.Persons;

[ApiController]
[Route("boards/{boardId:guid}/persons")]
[Authorize]
public class PersonsController(
    PersonsHandler handler,
    IValidator<CreatePersonRequest> createPersonValidator,
    IValidator<UpdatePersonRequest> updatePersonValidator)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid boardId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await handler.GetAllAsync(boardId, userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid boardId, [FromBody] CreatePersonRequest request)
    {
        var validation = await createPersonValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await handler.CreateAsync(boardId, request, userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : CreatedAtAction(nameof(GetAll), new { boardId }, result.Value);
    }

    [HttpPut("{personId:guid}")]
    public async Task<IActionResult> Update(
        Guid boardId,
        Guid personId,
        [FromBody] UpdatePersonRequest request)
    {
        var validation = await updatePersonValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await handler.UpdateAsync(boardId, personId, request, userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : Ok(result.Value);
    }

    [HttpDelete("{personId:guid}")]
    public async Task<IActionResult> Delete(Guid boardId, Guid personId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await handler.DeleteAsync(boardId, personId, userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : NoContent();
    }
}
