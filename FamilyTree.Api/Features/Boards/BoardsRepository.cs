using System.Data;
using Dapper;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;

namespace FamilyTreeApiV2.Features.Boards;

public class BoardsRepository(IDbConnectionFactory dbConnectionFactory) : IBoardsRepository
{
    public async Task<Board> CreateBoardAsync(string name, IDbConnection connection, IDbTransaction transaction)
    {
        const string sql = @"
            INSERT INTO public.boards (name)
            VALUES (@Name)
            RETURNING id, name, created_at AS CreatedAt";

        var board = await connection.QuerySingleAsync<BoardInsertRow>(sql, new { Name = name }, transaction);
        return new Board(board.Id, board.Name, BoardRole.Owner, board.CreatedAt);
    }

    public async Task<IEnumerable<Board>> GetBoardsByUserIdAsync(Guid userId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT b.id, b.name, bm.role, b.created_at AS CreatedAt
            FROM public.boards b
            JOIN public.board_members bm ON bm.board_id = b.id
            WHERE bm.user_id = @UserId";

        return await connection.QueryAsync<Board>(sql, new { UserId = userId });
    }

    public async Task DeleteBoardAsync(Guid boardId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = "DELETE FROM public.boards WHERE id = @BoardId";

        await connection.ExecuteAsync(sql, new { BoardId = boardId });
    }

    private record BoardInsertRow(Guid Id, string Name, DateTime CreatedAt);
}
