using ErrorOr;
using FamilyTreeApiV2.Shared;

namespace FamilyTreeApiV2.Features.Members;

public class MembersHandler(IMembersRepository repository)
{
    public async Task<ErrorOr<List<MemberResponse>>> GetMembersAsync(Guid boardId, Guid callerId)
    {
        var role = await repository.GetCallerRoleAsync(boardId, callerId);
        if (role is null)
            return MembersErrors.BoardNotFound;

        var members = await repository.GetMembersAsync(boardId);
        return members.Select(ToResponse).ToList();
    }

    public async Task<ErrorOr<MemberResponse>> AddMemberAsync(Guid boardId, AddMemberRequest request, Guid callerId)
    {
        var callerRole = await repository.GetCallerRoleAsync(boardId, callerId);
        if (callerRole is null)
            return MembersErrors.BoardNotFound;
        if (callerRole != BoardRole.Owner)
            return MembersErrors.Forbidden;

        var targetUserId = await repository.GetUserIdByEmailAsync(request.Email);
        if (targetUserId is null)
            return MembersErrors.UserNotFound;

        var alreadyMember = await repository.IsMemberAsync(boardId, targetUserId.Value);
        if (alreadyMember)
            return MembersErrors.AlreadyMember;

        var member = await repository.AddMemberAsync(boardId, targetUserId.Value, request.Role);
        return ToResponse(member);
    }

    public async Task<ErrorOr<MemberResponse>> UpdateMemberRoleAsync(
        Guid boardId, Guid memberId, UpdateMemberRoleRequest request, Guid callerId)
    {
        var callerRole = await repository.GetCallerRoleAsync(boardId, callerId);
        if (callerRole is null)
            return MembersErrors.BoardNotFound;
        if (callerRole != BoardRole.Owner)
            return MembersErrors.Forbidden;

        var targetMember = await repository.GetMemberByIdAsync(boardId, memberId);
        if (targetMember is null)
            return MembersErrors.MemberNotFound;
        if (targetMember.UserId == callerId)
            return MembersErrors.CannotEditSelf;

        var updated = await repository.UpdateMemberRoleAsync(boardId, memberId, request.Role);
        if (updated is null)
            return MembersErrors.MemberNotFound;

        return ToResponse(updated);
    }

    public async Task<ErrorOr<MemberResponse>> UpdateViewerPrivacyModeAsync(
        Guid boardId, Guid memberId, UpdateViewerPrivacyModeRequest request, Guid callerId)
    {
        var callerRole = await repository.GetCallerRoleAsync(boardId, callerId);
        if (callerRole is null)
            return MembersErrors.BoardNotFound;
        if (callerRole != BoardRole.Owner)
            return MembersErrors.Forbidden;

        var targetMember = await repository.GetMemberByIdAsync(boardId, memberId);
        if (targetMember is null)
            return MembersErrors.MemberNotFound;
        if (targetMember.Role != BoardRole.Viewer)
            return MembersErrors.MemberIsNotViewer;

        var updated = await repository.UpdateViewerPrivacyModeAsync(boardId, memberId, request.ViewerPrivacyMode);
        if (updated is null)
            return MembersErrors.MemberNotFound;

        return ToResponse(updated);
    }

    public async Task<ErrorOr<Deleted>> RemoveMemberAsync(Guid boardId, Guid memberId, Guid callerId)
    {
        var callerRole = await repository.GetCallerRoleAsync(boardId, callerId);
        if (callerRole is null)
            return MembersErrors.BoardNotFound;
        if (callerRole != BoardRole.Owner)
            return MembersErrors.Forbidden;

        var targetMember = await repository.GetMemberByIdAsync(boardId, memberId);
        if (targetMember is null)
            return MembersErrors.MemberNotFound;
        if (targetMember.UserId == callerId)
            return MembersErrors.CannotRemoveSelf;

        var deleted = await repository.DeleteMemberAsync(boardId, memberId);
        if (!deleted)
            return MembersErrors.MemberNotFound;

        return Result.Deleted;
    }

    private static MemberResponse ToResponse(Member member) =>
        new(member.MemberId, member.UserId, member.FirstName, member.LastName, member.Email, member.Role, member.ViewerPrivacyMode, member.CreatedAt);
}
