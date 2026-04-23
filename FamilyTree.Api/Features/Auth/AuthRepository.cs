using Dapper;
using FamilyTreeApiV2.Infrastructure.Database;

namespace FamilyTreeApiV2.Features.Auth;

public class AuthRepository(IDbConnectionFactory dbConnectionFactory) : IAuthRepository
{
    public async Task<User> InsertUserAsync(Guid userId, string firstName, string lastName, string email)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            INSERT INTO public.users (id, first_name, last_name, email)
            VALUES (@UserId, @FirstName, @LastName, lower(@Email))
            RETURNING id AS Id, first_name AS FirstName, last_name AS LastName, email AS Email";

        return await connection.QuerySingleAsync<User>(sql, new { UserId = userId, FirstName = firstName, LastName = lastName, Email = email });
    }

    public async Task<User?> GetUserAsync(Guid userId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT id AS Id, first_name AS FirstName, last_name AS LastName, email AS Email
            FROM public.users
            WHERE id = @UserId";

        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { UserId = userId });
    }

    public async Task<bool> IsLastOwnerOfAnyBoardAsync(Guid userId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT EXISTS (
                SELECT 1
                FROM public.board_members bm
                JOIN public.boards b ON b.id = bm.board_id
                WHERE bm.user_id = @UserId
                  AND bm.role = 'owner'
                  AND (
                      SELECT COUNT(*) FROM public.board_members bm2
                      WHERE bm2.board_id = bm.board_id AND bm2.role = 'owner'
                  ) = 1
            )";

        return await connection.ExecuteScalarAsync<bool>(sql, new { UserId = userId });
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = "DELETE FROM public.users WHERE id = @UserId";

        await connection.ExecuteAsync(sql, new { UserId = userId });
    }
}
