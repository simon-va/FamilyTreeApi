using FamilyTreeApiV2.Shared;

namespace FamilyTreeApiV2.Features.Members;

public interface IMembersRepository
{
    Task<BoardRole?> GetCallerRoleAsync(Guid boardId, Guid userId);
    Task<IEnumerable<MemberRow>> GetMembersAsync(Guid boardId);
    Task<MemberRow?> GetMemberByIdAsync(Guid boardId, Guid memberId);
    Task<Guid?> GetUserIdByEmailAsync(string email);
    Task<bool> IsMemberAsync(Guid boardId, Guid userId);
    Task<MemberRow> AddMemberAsync(Guid boardId, Guid userId, BoardRole role);
    Task<MemberRow?> UpdateMemberRoleAsync(Guid boardId, Guid memberId, BoardRole role);
    Task<bool> DeleteMemberAsync(Guid boardId, Guid memberId);
}

public record MemberRow(
    Guid MemberId,
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    BoardRole Role,
    DateTime CreatedAt);
