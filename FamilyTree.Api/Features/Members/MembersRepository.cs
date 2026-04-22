using Dapper;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;

namespace FamilyTreeApiV2.Features.Members;

public class MembersRepository(IDbConnectionFactory dbConnectionFactory) : IMembersRepository
{
    public async Task<BoardRole?> GetCallerRoleAsync(Guid boardId, string userId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT bm.role
            FROM public.board_members bm
            JOIN public.boards b ON b.id = bm.board_id
            WHERE bm.board_id = @BoardId
              AND bm.user_id  = @UserId::uuid
              AND b.is_deleted = false";

        var roleString = await connection.ExecuteScalarAsync<string?>(sql, new { BoardId = boardId, UserId = userId });
        return roleString is null ? null : Enum.Parse<BoardRole>(roleString, ignoreCase: true);
    }

    public async Task<IEnumerable<MemberRow>> GetMembersAsync(Guid boardId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                bm.id         AS MemberId,
                bm.user_id    AS UserId,
                u.first_name  AS FirstName,
                u.last_name   AS LastName,
                u.email       AS Email,
                bm.role       AS Role,
                bm.created_at AS CreatedAt
            FROM public.board_members bm
            JOIN public.users u ON u.id = bm.user_id
            JOIN public.boards b ON b.id = bm.board_id
            WHERE bm.board_id = @BoardId
              AND b.is_deleted = false
            ORDER BY bm.created_at";

        return await connection.QueryAsync<MemberRow>(sql, new { BoardId = boardId });
    }

    public async Task<MemberRow?> GetMemberByIdAsync(Guid boardId, Guid memberId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                bm.id         AS MemberId,
                bm.user_id    AS UserId,
                u.first_name  AS FirstName,
                u.last_name   AS LastName,
                u.email       AS Email,
                bm.role       AS Role,
                bm.created_at AS CreatedAt
            FROM public.board_members bm
            JOIN public.users u ON u.id = bm.user_id
            JOIN public.boards b ON b.id = bm.board_id
            WHERE bm.board_id = @BoardId
              AND bm.id       = @MemberId
              AND b.is_deleted = false";

        return await connection.QuerySingleOrDefaultAsync<MemberRow>(sql,
            new { BoardId = boardId, MemberId = memberId });
    }

    public async Task<Guid?> GetUserIdByEmailAsync(string email)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT id
            FROM public.users
            WHERE email = lower(@Email)";

        return await connection.ExecuteScalarAsync<Guid?>(sql, new { Email = email });
    }

    public async Task<bool> IsMemberAsync(Guid boardId, Guid userId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT EXISTS (
                SELECT 1
                FROM public.board_members
                WHERE board_id = @BoardId
                  AND user_id  = @UserId
            )";

        return await connection.ExecuteScalarAsync<bool>(sql, new { BoardId = boardId, UserId = userId });
    }

    public async Task<MemberRow> AddMemberAsync(Guid boardId, Guid userId, BoardRole role)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            INSERT INTO public.board_members (board_id, user_id, role)
            VALUES (@BoardId, @UserId, @Role)
            RETURNING id AS MemberId";

        var newId = await connection.ExecuteScalarAsync<Guid>(sql,
            new { BoardId = boardId, UserId = userId, Role = role });

        return (await GetMemberByIdAsync(boardId, newId))!;
    }

    public async Task<MemberRow?> UpdateMemberRoleAsync(Guid boardId, Guid memberId, BoardRole role)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            UPDATE public.board_members
            SET role = @Role
            WHERE board_id = @BoardId
              AND id       = @MemberId
            RETURNING id";

        var updatedMemberId = await connection.ExecuteScalarAsync<Guid?>(sql,
            new { BoardId = boardId, MemberId = memberId, Role = role });

        return updatedMemberId is null ? null : await GetMemberByIdAsync(boardId, memberId);
    }

    public async Task<bool> DeleteMemberAsync(Guid boardId, Guid memberId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            DELETE FROM public.board_members
            WHERE board_id = @BoardId
              AND id       = @MemberId";

        var deletedCount = await connection.ExecuteAsync(sql,
            new { BoardId = boardId, MemberId = memberId });

        return deletedCount > 0;
    }
}
