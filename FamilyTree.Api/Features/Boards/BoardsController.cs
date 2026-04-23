using FamilyTreeApiV2.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTreeApiV2.Features.Boards;

[ApiController]
[Route("boards")]
[Authorize]
public class BoardsController(
    BoardsHandler handler,
    IValidator<CreateBoardRequest> createBoardValidator)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateBoard([FromBody] CreateBoardRequest request)
    {
        var validation = await createBoardValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));

        var userId = User.GetUserId();
        var result = await handler.CreateBoardAsync(request, userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : CreatedAtAction(nameof(GetBoards), result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> GetBoards()
    {
        var userId = User.GetUserId();
        var result = await handler.GetBoardsAsync(userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteBoard(Guid id)
    {
        var userId = User.GetUserId();
        var result = await handler.DeleteBoardAsync(id, userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : NoContent();
    }
}
