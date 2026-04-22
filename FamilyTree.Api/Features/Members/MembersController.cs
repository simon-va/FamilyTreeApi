using System.Security.Claims;
using FamilyTreeApiV2.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTreeApiV2.Features.Members;

[ApiController]
[Route("boards/{boardId:guid}/members")]
[Authorize]
public class MembersController(
    MembersHandler handler,
    IValidator<AddMemberRequest> addMemberValidator,
    IValidator<UpdateMemberRoleRequest> updateMemberRoleValidator)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMembers(Guid boardId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await handler.GetMembersAsync(boardId, userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> AddMember(Guid boardId, [FromBody] AddMemberRequest request)
    {
        var validation = await addMemberValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await handler.AddMemberAsync(boardId, request, userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : CreatedAtAction(nameof(GetMembers), new { boardId }, result.Value);
    }

    [HttpPut("{memberId:guid}")]
    public async Task<IActionResult> UpdateMemberRole(
        Guid boardId,
        Guid memberId,
        [FromBody] UpdateMemberRoleRequest request)
    {
        var validation = await updateMemberRoleValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await handler.UpdateMemberRoleAsync(boardId, memberId, request, userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : Ok(result.Value);
    }

    [HttpDelete("{memberId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid boardId, Guid memberId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await handler.RemoveMemberAsync(boardId, memberId, userId);

        return result.IsError
            ? ErrorMapper.ToActionResult(result.Errors, this)
            : NoContent();
    }
}
