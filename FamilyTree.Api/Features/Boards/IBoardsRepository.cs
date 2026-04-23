using FamilyTreeApiV2.Shared;

namespace FamilyTreeApiV2.Features.Boards;

public interface IBoardsRepository
{
    Task<Board> CreateBoardAsync(string name, Guid userId);
    Task<IEnumerable<Board>> GetBoardsByUserIdAsync(Guid userId);
    Task<BoardRole?> GetUserRoleOnBoardAsync(Guid boardId, Guid userId);
    Task DeleteBoardAsync(Guid boardId);
}
