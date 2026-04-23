using Dapper;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;

namespace FamilyTreeApiV2.Features.Boards;

public class BoardsRepository(IDbConnectionFactory dbConnectionFactory) : IBoardsRepository
{
    public async Task<BoardRow> CreateBoardAsync(string name, Guid userId)
    {
        using var connection = dbConnectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        const string insertBoard = @"
            INSERT INTO public.boards (name)
            VALUES (@Name)
            RETURNING id, name, created_at AS CreatedAt";

        var board = await connection.QuerySingleAsync<BoardInsertRow>(insertBoard, new { Name = name }, transaction);

        const string insertMember = @"
            INSERT INTO public.board_members (board_id, user_id, role)
            VALUES (@BoardId, @UserId, 'owner')";

        await connection.ExecuteAsync(insertMember, new { BoardId = board.Id, UserId = userId }, transaction);

        transaction.Commit();

        return new BoardRow(board.Id, board.Name, BoardRole.Owner, board.CreatedAt);
    }

    public async Task<IEnumerable<BoardRow>> GetBoardsByUserIdAsync(Guid userId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT b.id, b.name, bm.role, b.created_at AS CreatedAt
            FROM public.boards b
            JOIN public.board_members bm ON bm.board_id = b.id
            WHERE bm.user_id = @UserId";

        return await connection.QueryAsync<BoardRow>(sql, new { UserId = userId });
    }

    public async Task<BoardRole?> GetUserRoleOnBoardAsync(Guid boardId, Guid userId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT role
            FROM public.board_members
            WHERE board_id = @BoardId
              AND user_id  = @UserId";

        var roleString = await connection.ExecuteScalarAsync<string?>(sql, new { BoardId = boardId, UserId = userId });
        return roleString is null ? null : Enum.Parse<BoardRole>(roleString, ignoreCase: true);
    }

    public async Task DeleteBoardAsync(Guid boardId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = "DELETE FROM public.boards WHERE id = @BoardId";

        await connection.ExecuteAsync(sql, new { BoardId = boardId });
    }

    private record BoardInsertRow(Guid Id, string Name, DateTime CreatedAt);
}
