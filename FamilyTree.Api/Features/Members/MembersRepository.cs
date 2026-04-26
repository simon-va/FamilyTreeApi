using System.Data;
using Dapper;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;

namespace FamilyTreeApiV2.Features.Members;

public class MembersRepository(IDbConnectionFactory dbConnectionFactory) : IMembersRepository
{
    public async Task<BoardRole?> GetCallerRoleAsync(Guid boardId, Guid userId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT role
            FROM public.board_members
            WHERE board_id = @BoardId
              AND user_id  = @UserId";

        return await connection.QuerySingleOrDefaultAsync<BoardRole?>(sql, new { BoardId = boardId, UserId = userId });
    }

    public async Task<IEnumerable<Member>> GetMembersAsync(Guid boardId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                bm.id                  AS MemberId,
                bm.user_id             AS UserId,
                u.first_name           AS FirstName,
                u.last_name            AS LastName,
                u.email                AS Email,
                bm.role                AS Role,
                bm.viewer_privacy_mode AS ViewerPrivacyMode,
                bm.created_at          AS CreatedAt
            FROM public.board_members bm
            JOIN public.users u ON u.id = bm.user_id
            WHERE bm.board_id = @BoardId
            ORDER BY bm.created_at";

        return await connection.QueryAsync<Member>(sql, new { BoardId = boardId });
    }

    public async Task<Member?> GetMemberByIdAsync(Guid boardId, Guid memberId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                bm.id                  AS MemberId,
                bm.user_id             AS UserId,
                u.first_name           AS FirstName,
                u.last_name            AS LastName,
                u.email                AS Email,
                bm.role                AS Role,
                bm.viewer_privacy_mode AS ViewerPrivacyMode,
                bm.created_at          AS CreatedAt
            FROM public.board_members bm
            JOIN public.users u ON u.id = bm.user_id
            WHERE bm.board_id = @BoardId
              AND bm.id       = @MemberId";

        return await connection.QuerySingleOrDefaultAsync<Member>(sql,
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

    public async Task AddOwnerAsync(Guid boardId, Guid userId, IDbConnection connection, IDbTransaction transaction)
    {
        const string sql = @"
            INSERT INTO public.board_members (board_id, user_id, role)
            VALUES (@BoardId, @UserId, @Role)";

        await connection.ExecuteAsync(sql, new { BoardId = boardId, UserId = userId, Role = BoardRole.Owner }, transaction);
    }

    public async Task<Member> AddMemberAsync(Guid boardId, Guid userId, BoardRole role)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            INSERT INTO public.board_members (board_id, user_id, role, viewer_privacy_mode)
            VALUES (@BoardId, @UserId, @Role, @ViewerPrivacyMode)
            RETURNING id AS MemberId";

        ViewerPrivacyMode? viewerPrivacyMode = role == BoardRole.Viewer ? ViewerPrivacyMode.Restricted : null;
        var newId = await connection.ExecuteScalarAsync<Guid>(sql,
            new { BoardId = boardId, UserId = userId, Role = role, ViewerPrivacyMode = viewerPrivacyMode });

        return (await GetMemberByIdAsync(boardId, newId))!;
    }

    public async Task<Member?> UpdateMemberRoleAsync(Guid boardId, Guid memberId, BoardRole role)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            UPDATE public.board_members
            SET role                = @Role,
                viewer_privacy_mode = @ViewerPrivacyMode
            WHERE board_id = @BoardId
              AND id       = @MemberId
            RETURNING id";

        ViewerPrivacyMode? viewerPrivacyMode = role == BoardRole.Viewer ? ViewerPrivacyMode.Restricted : null;
        var updatedMemberId = await connection.ExecuteScalarAsync<Guid?>(sql,
            new { BoardId = boardId, MemberId = memberId, Role = role, ViewerPrivacyMode = viewerPrivacyMode });

        return updatedMemberId is null ? null : await GetMemberByIdAsync(boardId, memberId);
    }

    public async Task<ViewerPrivacyMode> GetCallerPrivacyModeAsync(Guid boardId, Guid userId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT viewer_privacy_mode
            FROM public.board_members
            WHERE board_id = @BoardId
              AND user_id  = @UserId";

        var mode = await connection.QuerySingleOrDefaultAsync<ViewerPrivacyMode?>(sql,
            new { BoardId = boardId, UserId = userId });

        return mode ?? ViewerPrivacyMode.Restricted;
    }

    public async Task<Member?> UpdateViewerPrivacyModeAsync(Guid boardId, Guid memberId, ViewerPrivacyMode mode)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            UPDATE public.board_members
            SET viewer_privacy_mode = @Mode
            WHERE board_id = @BoardId
              AND id       = @MemberId
            RETURNING id";

        var updatedMemberId = await connection.ExecuteScalarAsync<Guid?>(sql,
            new { BoardId = boardId, MemberId = memberId, Mode = mode });

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
