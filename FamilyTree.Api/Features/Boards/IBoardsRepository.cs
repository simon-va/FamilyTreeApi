using FamilyTreeApiV2.Shared;

namespace FamilyTreeApiV2.Features.Boards;

public interface IBoardsRepository
{
    Task<BoardRow> CreateBoardAsync(string name, Guid userId);
    Task<IEnumerable<BoardRow>> GetBoardsByUserIdAsync(Guid userId);
    Task<BoardRole?> GetUserRoleOnBoardAsync(Guid boardId, Guid userId);
    Task SoftDeleteBoardAsync(Guid boardId);
}

public record BoardRow(Guid Id, string Name, BoardRole Role, DateTime CreatedAt);
