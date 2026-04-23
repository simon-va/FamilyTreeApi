using System.Data;
using Dapper;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;

namespace FamilyTreeApiV2.Features.Persons;

public class PersonsRepository(IDbConnectionFactory dbConnectionFactory) : IPersonsRepository
{
    public async Task<BoardRole?> GetCallerRoleAsync(Guid boardId, Guid userId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT bm.role
            FROM public.board_members bm
            JOIN public.boards b ON b.id = bm.board_id
            WHERE bm.board_id = @BoardId
              AND bm.user_id  = @UserId
              AND b.is_deleted = false";

        var roleString = await connection.ExecuteScalarAsync<string?>(sql, new { BoardId = boardId, UserId = userId });
        return roleString is null ? null : Enum.Parse<BoardRole>(roleString, ignoreCase: true);
    }

    public async Task<IEnumerable<(PersonRow Person, FuzzyDate? BirthDate, FuzzyDate? DeathDate)>> GetAllAsync(Guid boardId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                p.id              AS Id,
                p.board_id        AS BoardId,
                p.first_name      AS FirstName,
                p.last_name       AS LastName,
                p.middle_names    AS MiddleNames,
                p.birth_name      AS BirthName,
                p.gender          AS Gender,
                p.birth_place     AS BirthPlace,
                p.death_place     AS DeathPlace,
                p.burial_place    AS BurialPlace,
                p.title           AS Title,
                p.religion        AS Religion,
                p.notes           AS Notes,
                p.created_at      AS CreatedAt,
                p.birth_date_id   AS BirthDateId,
                p.death_date_id   AS DeathDateId,
                bd.id             AS Id,
                bd.precision      AS Precision,
                bd.date           AS Date,
                bd.date_precision AS DatePrecision,
                bd.date_to        AS DateTo,
                bd.date_to_precision AS DateToPrecision,
                bd.note           AS Note,
                bd.created_at     AS CreatedAt,
                dd.id             AS Id,
                dd.precision      AS Precision,
                dd.date           AS Date,
                dd.date_precision AS DatePrecision,
                dd.date_to        AS DateTo,
                dd.date_to_precision AS DateToPrecision,
                dd.note           AS Note,
                dd.created_at     AS CreatedAt
            FROM public.persons p
            LEFT JOIN public.fuzzy_dates bd ON p.birth_date_id = bd.id
            LEFT JOIN public.fuzzy_dates dd ON p.death_date_id = dd.id
            WHERE p.board_id   = @BoardId
              AND p.is_deleted = false
            ORDER BY p.last_name, p.first_name";

        return await connection.QueryAsync<PersonRow, FuzzyDate, FuzzyDate, (PersonRow, FuzzyDate?, FuzzyDate?)>(
            sql,
            (person, birthDate, deathDate) => (
                person,
                birthDate?.Id == Guid.Empty ? null : birthDate,
                deathDate?.Id == Guid.Empty ? null : deathDate),
            new { BoardId = boardId },
            splitOn: "Id,Id");
    }

    public async Task<PersonRow?> GetByIdAsync(Guid boardId, Guid personId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                id              AS Id,
                board_id        AS BoardId,
                first_name      AS FirstName,
                last_name       AS LastName,
                middle_names    AS MiddleNames,
                birth_name      AS BirthName,
                gender          AS Gender,
                birth_place     AS BirthPlace,
                death_place     AS DeathPlace,
                burial_place    AS BurialPlace,
                title           AS Title,
                religion        AS Religion,
                notes           AS Notes,
                created_at      AS CreatedAt,
                birth_date_id   AS BirthDateId,
                death_date_id   AS DeathDateId
            FROM public.persons
            WHERE id         = @PersonId
              AND board_id   = @BoardId
              AND is_deleted = false";

        return await connection.QuerySingleOrDefaultAsync<PersonRow>(sql,
            new { PersonId = personId, BoardId = boardId });
    }

    public async Task<PersonRow> CreateAsync(
        Guid boardId,
        CreatePersonRequest request,
        Guid? birthDateId,
        Guid? deathDateId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        const string sql = @"
            INSERT INTO public.persons
                (board_id, first_name, last_name, middle_names, birth_name, gender,
                 birth_place, death_place, burial_place, title, religion, notes,
                 birth_date_id, death_date_id)
            VALUES
                (@BoardId, @FirstName, @LastName, @MiddleNames, @BirthName, @Gender,
                 @BirthPlace, @DeathPlace, @BurialPlace, @Title, @Religion, @Notes,
                 @BirthDateId, @DeathDateId)
            RETURNING
                id              AS Id,
                board_id        AS BoardId,
                first_name      AS FirstName,
                last_name       AS LastName,
                middle_names    AS MiddleNames,
                birth_name      AS BirthName,
                gender          AS Gender,
                birth_place     AS BirthPlace,
                death_place     AS DeathPlace,
                burial_place    AS BurialPlace,
                title           AS Title,
                religion        AS Religion,
                notes           AS Notes,
                created_at      AS CreatedAt,
                birth_date_id   AS BirthDateId,
                death_date_id   AS DeathDateId";

        return await connection.QuerySingleAsync<PersonRow>(sql, new
        {
            BoardId = boardId,
            request.FirstName,
            request.LastName,
            request.MiddleNames,
            request.BirthName,
            request.Gender,
            request.BirthPlace,
            request.DeathPlace,
            request.BurialPlace,
            request.Title,
            request.Religion,
            request.Notes,
            BirthDateId = birthDateId,
            DeathDateId = deathDateId
        }, transaction);
    }

    public async Task<PersonRow?> UpdateAsync(
        Guid boardId,
        Guid personId,
        UpdatePersonRequest request,
        Guid? birthDateId,
        Guid? deathDateId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        const string sql = @"
            UPDATE public.persons
            SET
                first_name    = @FirstName,
                last_name     = @LastName,
                middle_names  = @MiddleNames,
                birth_name    = @BirthName,
                gender        = @Gender,
                birth_place   = @BirthPlace,
                death_place   = @DeathPlace,
                burial_place  = @BurialPlace,
                title         = @Title,
                religion      = @Religion,
                notes         = @Notes,
                birth_date_id = @BirthDateId,
                death_date_id = @DeathDateId
            WHERE id         = @PersonId
              AND board_id   = @BoardId
              AND is_deleted = false
            RETURNING
                id              AS Id,
                board_id        AS BoardId,
                first_name      AS FirstName,
                last_name       AS LastName,
                middle_names    AS MiddleNames,
                birth_name      AS BirthName,
                gender          AS Gender,
                birth_place     AS BirthPlace,
                death_place     AS DeathPlace,
                burial_place    AS BurialPlace,
                title           AS Title,
                religion        AS Religion,
                notes           AS Notes,
                created_at      AS CreatedAt,
                birth_date_id   AS BirthDateId,
                death_date_id   AS DeathDateId";

        return await connection.QuerySingleOrDefaultAsync<PersonRow>(sql, new
        {
            PersonId = personId,
            BoardId = boardId,
            request.FirstName,
            request.LastName,
            request.MiddleNames,
            request.BirthName,
            request.Gender,
            request.BirthPlace,
            request.DeathPlace,
            request.BurialPlace,
            request.Title,
            request.Religion,
            request.Notes,
            BirthDateId = birthDateId,
            DeathDateId = deathDateId
        }, transaction);
    }

    public async Task<bool> DeleteAsync(Guid boardId, Guid personId)
    {
        using var connection = dbConnectionFactory.CreateConnection();

        const string sql = @"
            UPDATE public.persons
            SET is_deleted = true,
                deleted_at = now()
            WHERE id         = @PersonId
              AND board_id   = @BoardId
              AND is_deleted = false";

        var deletedCount = await connection.ExecuteAsync(sql,
            new { PersonId = personId, BoardId = boardId });

        return deletedCount > 0;
    }
}
