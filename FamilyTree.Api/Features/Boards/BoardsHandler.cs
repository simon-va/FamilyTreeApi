using ErrorOr;
using FamilyTreeApiV2.Shared;

namespace FamilyTreeApiV2.Features.Boards;

public class BoardsHandler(IBoardsRepository repository)
{
    public async Task<ErrorOr<BoardResponse>> CreateBoardAsync(CreateBoardRequest request, Guid userId)
    {
        var board = await repository.CreateBoardAsync(request.Name, userId);

        return new BoardResponse(board.Id, board.Name, board.Role, board.CreatedAt);
    }

    public async Task<ErrorOr<List<BoardResponse>>> GetBoardsAsync(Guid userId)
    {
        var boards = await repository.GetBoardsByUserIdAsync(userId);

        return boards
            .Select(b => new BoardResponse(b.Id, b.Name, b.Role, b.CreatedAt))
            .ToList();
    }

    public async Task<ErrorOr<Deleted>> DeleteBoardAsync(Guid boardId, Guid userId)
    {
        var role = await repository.GetUserRoleOnBoardAsync(boardId, userId);

        if (role is null)
            return BoardsErrors.NotFound;

        if (role != BoardRole.Owner)
            return BoardsErrors.Forbidden;

        await repository.DeleteBoardAsync(boardId);

        return Result.Deleted;
    }
}
