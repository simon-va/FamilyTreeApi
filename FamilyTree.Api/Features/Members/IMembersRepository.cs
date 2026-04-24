using System.Data;
using FamilyTreeApiV2.Shared;

namespace FamilyTreeApiV2.Features.Members;

public interface IMembersRepository
{
    Task<BoardRole?> GetCallerRoleAsync(Guid boardId, Guid userId);
    Task<IEnumerable<Member>> GetMembersAsync(Guid boardId);
    Task<Member?> GetMemberByIdAsync(Guid boardId, Guid memberId);
    Task<Guid?> GetUserIdByEmailAsync(string email);
    Task<bool> IsMemberAsync(Guid boardId, Guid userId);
    Task AddOwnerAsync(Guid boardId, Guid userId, IDbConnection connection, IDbTransaction transaction);
    Task<Member> AddMemberAsync(Guid boardId, Guid userId, BoardRole role);
    Task<Member?> UpdateMemberRoleAsync(Guid boardId, Guid memberId, BoardRole role);
    Task<ViewerPrivacyMode> GetCallerPrivacyModeAsync(Guid boardId, Guid userId);
    Task<Member?> UpdateViewerPrivacyModeAsync(Guid boardId, Guid memberId, ViewerPrivacyMode mode);
    Task<bool> DeleteMemberAsync(Guid boardId, Guid memberId);
}
