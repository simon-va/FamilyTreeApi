using Dapper;
using FamilyTreeApiV2.Infrastructure.Database;

namespace FamilyTreeApiV2.Features.Auth;

public class AuthRepository(IDbConnectionFactory dbConnectionFactory) : IAuthRepository
{
    public async Task InsertUserAsync(string userId, string firstName, string lastName, string email)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            INSERT INTO public.users (id, first_name, last_name, email)
            VALUES (@UserId::uuid, @FirstName, @LastName, lower(@Email))";

        await connection.ExecuteAsync(sql, new { UserId = userId, FirstName = firstName, LastName = lastName, Email = email });
    }

    public async Task<(string FirstName, string LastName)?> GetUserNamesAsync(string userId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT first_name AS FirstName, last_name AS LastName
            FROM public.users
            WHERE id = @UserId::uuid";
        
        var userNames = await connection.QuerySingleOrDefaultAsync<UserNameRow>(sql, new { UserId = userId });

        return userNames is null ? null : (userNames.FirstName, userNames.LastName);
    }

    private record UserNameRow(string FirstName, string LastName);
}
