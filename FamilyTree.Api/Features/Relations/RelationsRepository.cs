using System.Data;
using Dapper;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared.FuzzyDates;

namespace FamilyTreeApiV2.Features.Relations;

public class RelationsRepository(IDbConnectionFactory dbConnectionFactory) : IRelationsRepository
{
    public async Task<IEnumerable<(Relation Relation, FuzzyDate? StartDate, FuzzyDate? EndDate)>> GetAllAsync(Guid boardId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                r.id            AS Id,
                r.board_id      AS BoardId,
                r.person_a_id   AS PersonAId,
                r.person_b_id   AS PersonBId,
                r.type          AS Type,
                r.created_at    AS CreatedAt,
                r.start_date_id AS StartDateId,
                r.end_date_id   AS EndDateId,
                r.end_reason    AS EndReason,
                r.notes         AS Notes,
                sd.id               AS Id,
                sd.precision        AS Precision,
                sd.date             AS Date,
                sd.date_precision   AS DatePrecision,
                sd.date_to          AS DateTo,
                sd.date_to_precision AS DateToPrecision,
                sd.note             AS Note,
                sd.created_at       AS CreatedAt,
                ed.id               AS Id,
                ed.precision        AS Precision,
                ed.date             AS Date,
                ed.date_precision   AS DatePrecision,
                ed.date_to          AS DateTo,
                ed.date_to_precision AS DateToPrecision,
                ed.note             AS Note,
                ed.created_at       AS CreatedAt
            FROM public.relations r
            LEFT JOIN public.fuzzy_dates sd ON r.start_date_id = sd.id
            LEFT JOIN public.fuzzy_dates ed ON r.end_date_id = ed.id
            WHERE r.board_id = @BoardId";

        return await connection.QueryAsync<Relation, FuzzyDate, FuzzyDate, (Relation, FuzzyDate?, FuzzyDate?)>(
            sql,
            (relation, startDate, endDate) => (
                relation,
                startDate?.Id == Guid.Empty ? null : startDate,
                endDate?.Id == Guid.Empty ? null : endDate),
            new { BoardId = boardId },
            splitOn: "Id,Id");
    }

    public async Task<bool> DoPersonsBelongToBoardAsync(Guid boardId, Guid personAId, Guid personBId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT COUNT(*)
            FROM public.persons
            WHERE board_id = @BoardId
              AND id IN (@PersonAId, @PersonBId)";

        var count = await connection.ExecuteScalarAsync<int>(sql,
            new { BoardId = boardId, PersonAId = personAId, PersonBId = personBId });

        return count == 2;
    }

    public async Task<Relation?> GetByIdAsync(Guid boardId, Guid relationId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                id            AS Id,
                board_id      AS BoardId,
                person_a_id   AS PersonAId,
                person_b_id   AS PersonBId,
                type          AS Type,
                created_at    AS CreatedAt,
                start_date_id AS StartDateId,
                end_date_id   AS EndDateId,
                end_reason    AS EndReason,
                notes         AS Notes
            FROM public.relations
            WHERE id       = @RelationId
              AND board_id = @BoardId";

        return await connection.QuerySingleOrDefaultAsync<Relation>(sql,
            new { RelationId = relationId, BoardId = boardId });
    }

    public async Task<Relation> CreateAsync(
        Guid boardId,
        CreateRelationRequest request,
        Guid? startDateId,
        Guid? endDateId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        const string sql = @"
            INSERT INTO public.relations
                (board_id, person_a_id, person_b_id, type, start_date_id, end_date_id, end_reason, notes)
            VALUES
                (@BoardId, @PersonAId, @PersonBId, @Type, @StartDateId, @EndDateId, @EndReason, @Notes)
            RETURNING
                id            AS Id,
                board_id      AS BoardId,
                person_a_id   AS PersonAId,
                person_b_id   AS PersonBId,
                type          AS Type,
                created_at    AS CreatedAt,
                start_date_id AS StartDateId,
                end_date_id   AS EndDateId,
                end_reason    AS EndReason,
                notes         AS Notes";

        return await connection.QuerySingleAsync<Relation>(sql, new
        {
            BoardId = boardId,
            request.PersonAId,
            request.PersonBId,
            request.Type,
            StartDateId = startDateId,
            EndDateId = endDateId,
            request.EndReason,
            request.Notes
        }, transaction);
    }

    public async Task<Relation?> UpdateAsync(
        Guid boardId,
        Guid relationId,
        UpdateRelationRequest request,
        Guid? startDateId,
        Guid? endDateId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        const string sql = @"
            UPDATE public.relations
            SET
                type          = @Type,
                start_date_id = @StartDateId,
                end_date_id   = @EndDateId,
                end_reason    = @EndReason,
                notes         = @Notes
            WHERE id       = @RelationId
              AND board_id = @BoardId
            RETURNING
                id            AS Id,
                board_id      AS BoardId,
                person_a_id   AS PersonAId,
                person_b_id   AS PersonBId,
                type          AS Type,
                created_at    AS CreatedAt,
                start_date_id AS StartDateId,
                end_date_id   AS EndDateId,
                end_reason    AS EndReason,
                notes         AS Notes";

        return await connection.QuerySingleOrDefaultAsync<Relation>(sql, new
        {
            RelationId = relationId,
            BoardId = boardId,
            request.Type,
            StartDateId = startDateId,
            EndDateId = endDateId,
            request.EndReason,
            request.Notes
        }, transaction);
    }

    public async Task<bool> DeleteAsync(Guid boardId, Guid relationId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            DELETE FROM public.relations
            WHERE id       = @RelationId
              AND board_id = @BoardId";

        var deletedCount = await connection.ExecuteAsync(sql,
            new { RelationId = relationId, BoardId = boardId });

        return deletedCount > 0;
    }
}
