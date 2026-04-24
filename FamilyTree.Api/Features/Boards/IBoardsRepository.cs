using System.Data;
using FamilyTreeApiV2.Shared;

namespace FamilyTreeApiV2.Features.Boards;

public interface IBoardsRepository
{
    Task<Board> CreateBoardAsync(string name, IDbConnection connection, IDbTransaction transaction);
    Task<IEnumerable<Board>> GetBoardsByUserIdAsync(Guid userId);
    Task DeleteBoardAsync(Guid boardId);
}
