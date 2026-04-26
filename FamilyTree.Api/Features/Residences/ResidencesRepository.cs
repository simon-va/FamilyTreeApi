using System.Data;
using Dapper;
using FamilyTreeApiV2.Infrastructure.Database;

namespace FamilyTreeApiV2.Features.Residences;

public class ResidencesRepository(IDbConnectionFactory dbConnectionFactory) : IResidencesRepository
{
    public async Task<bool> DoesPersonBelongToBoardAsync(Guid boardId, Guid personId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT 1
            FROM public.persons
            WHERE id       = @PersonId
              AND board_id = @BoardId";

        var result = await connection.QuerySingleOrDefaultAsync<int?>(sql, new { PersonId = personId, BoardId = boardId });
        return result is not null;
    }

    public async Task<IEnumerable<Residence>> GetAllAsync(Guid boardId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                id            AS Id,
                board_id      AS BoardId,
                person_id     AS PersonId,
                city          AS City,
                country       AS Country,
                street        AS Street,
                notes         AS Notes,
                start_date_id AS StartDateId,
                end_date_id   AS EndDateId,
                created_at    AS CreatedAt
            FROM public.residences
            WHERE board_id = @BoardId
            ORDER BY created_at";

        return await connection.QueryAsync<Residence>(sql, new { BoardId = boardId });
    }

    public async Task<Residence?> GetByIdAsync(Guid boardId, Guid residenceId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                id            AS Id,
                board_id      AS BoardId,
                person_id     AS PersonId,
                city          AS City,
                country       AS Country,
                street        AS Street,
                notes         AS Notes,
                start_date_id AS StartDateId,
                end_date_id   AS EndDateId,
                created_at    AS CreatedAt
            FROM public.residences
            WHERE id       = @ResidenceId
              AND board_id = @BoardId";

        return await connection.QuerySingleOrDefaultAsync<Residence>(sql,
            new { ResidenceId = residenceId, BoardId = boardId });
    }

    public async Task<Residence> CreateAsync(
        Guid boardId,
        CreateResidenceRequest request,
        Guid? startDateId,
        Guid? endDateId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        const string sql = @"
            INSERT INTO public.residences
                (board_id, person_id, city, country, street, notes, start_date_id, end_date_id)
            VALUES
                (@BoardId, @PersonId, @City, @Country, @Street, @Notes, @StartDateId, @EndDateId)
            RETURNING
                id            AS Id,
                board_id      AS BoardId,
                person_id     AS PersonId,
                city          AS City,
                country       AS Country,
                street        AS Street,
                notes         AS Notes,
                start_date_id AS StartDateId,
                end_date_id   AS EndDateId,
                created_at    AS CreatedAt";

        return await connection.QuerySingleAsync<Residence>(sql, new
        {
            BoardId = boardId,
            request.PersonId,
            request.City,
            request.Country,
            request.Street,
            request.Notes,
            StartDateId = startDateId,
            EndDateId = endDateId
        }, transaction);
    }

    public async Task<Residence?> UpdateAsync(
        Guid boardId,
        Guid residenceId,
        UpdateResidenceRequest request,
        Guid? startDateId,
        Guid? endDateId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        const string sql = @"
            UPDATE public.residences
            SET
                city          = @City,
                country       = @Country,
                street        = @Street,
                notes         = @Notes,
                start_date_id = @StartDateId,
                end_date_id   = @EndDateId
            WHERE id       = @ResidenceId
              AND board_id = @BoardId
            RETURNING
                id            AS Id,
                board_id      AS BoardId,
                person_id     AS PersonId,
                city          AS City,
                country       AS Country,
                street        AS Street,
                notes         AS Notes,
                start_date_id AS StartDateId,
                end_date_id   AS EndDateId,
                created_at    AS CreatedAt";

        return await connection.QuerySingleOrDefaultAsync<Residence>(sql, new
        {
            ResidenceId = residenceId,
            BoardId = boardId,
            request.City,
            request.Country,
            request.Street,
            request.Notes,
            StartDateId = startDateId,
            EndDateId = endDateId
        }, transaction);
    }

    public async Task<bool> DeleteAsync(Guid boardId, Guid residenceId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            DELETE FROM public.residences
            WHERE id       = @ResidenceId
              AND board_id = @BoardId";

        var deletedCount = await connection.ExecuteAsync(sql,
            new { ResidenceId = residenceId, BoardId = boardId });

        return deletedCount > 0;
    }
}
