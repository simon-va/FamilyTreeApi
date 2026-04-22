using FamilyTreeApiV2.Shared;

namespace FamilyTreeApiV2.Features.Boards;

public interface IBoardsRepository
{
    Task<BoardRow> CreateBoardAsync(string name, string userId);
    Task<IEnumerable<BoardRow>> GetBoardsByUserIdAsync(string userId);
    Task<BoardRole?> GetUserRoleOnBoardAsync(Guid boardId, string userId);
    Task SoftDeleteBoardAsync(Guid boardId);
}

public record BoardRow(Guid Id, string Name, BoardRole Role, DateTime CreatedAt);
