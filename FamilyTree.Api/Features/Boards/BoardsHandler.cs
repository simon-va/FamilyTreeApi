using ErrorOr;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Features.Members;
using FamilyTreeApiV2.Shared;

namespace FamilyTreeApiV2.Features.Boards;

public class BoardsHandler(IBoardsRepository boardsRepository, IMembersRepository membersRepository, IDbConnectionFactory connectionFactory)
{
    public async Task<ErrorOr<BoardResponse>> CreateBoardAsync(CreateBoardRequest request, Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        var board = await boardsRepository.CreateBoardAsync(request.Name, connection, transaction);
        await membersRepository.AddOwnerAsync(board.Id, userId, connection, transaction);

        transaction.Commit();

        return new BoardResponse(board.Id, board.Name, BoardRole.Owner, board.CreatedAt);
    }

    public async Task<ErrorOr<List<BoardResponse>>> GetBoardsAsync(Guid userId)
    {
        var boards = await boardsRepository.GetBoardsByUserIdAsync(userId);

        return boards
            .Select(b => new BoardResponse(b.Id, b.Name, b.Role, b.CreatedAt))
            .ToList();
    }

    public async Task<ErrorOr<Deleted>> DeleteBoardAsync(Guid boardId, Guid userId)
    {
        var role = await membersRepository.GetCallerRoleAsync(boardId, userId);

        if (role is null)
            return BoardsErrors.NotFound;

        if (role != BoardRole.Owner)
            return BoardsErrors.Forbidden;

        await boardsRepository.DeleteBoardAsync(boardId);

        return Result.Deleted;
    }
}
