using System.Data;
using Dapper;

namespace FamilyTreeApiV2.Shared.FuzzyDates;

public class FuzzyDateRepository : IFuzzyDateRepository
{
    public async Task CreateAsync(FuzzyDate fuzzyDate, IDbConnection connection, IDbTransaction transaction)
    {
        const string sql = @"
            INSERT INTO public.fuzzy_dates
                (id, precision, date, date_precision, date_to, date_to_precision, note)
            VALUES
                (@Id, @Precision, @Date, @DatePrecision, @DateTo, @DateToPrecision, @Note)";

        await connection.ExecuteAsync(sql, new
        {
            fuzzyDate.Id,
            fuzzyDate.Precision,
            fuzzyDate.Date,
            fuzzyDate.DatePrecision,
            fuzzyDate.DateTo,
            fuzzyDate.DateToPrecision,
            fuzzyDate.Note
        }, transaction);
    }

    public async Task<FuzzyDate?> GetByIdAsync(Guid id, IDbConnection connection, IDbTransaction transaction)
    {
        const string sql = @"
            SELECT
                id                AS Id,
                precision         AS Precision,
                date              AS Date,
                date_precision    AS DatePrecision,
                date_to           AS DateTo,
                date_to_precision AS DateToPrecision,
                note              AS Note,
                created_at        AS CreatedAt
            FROM public.fuzzy_dates
            WHERE id = @Id";

        return await connection.QuerySingleOrDefaultAsync<FuzzyDate>(sql, new { Id = id }, transaction);
    }

    public async Task UpdateAsync(FuzzyDate fuzzyDate, IDbConnection connection, IDbTransaction transaction)
    {
        const string sql = @"
            UPDATE public.fuzzy_dates
            SET
                precision         = @Precision,
                date              = @Date,
                date_precision    = @DatePrecision,
                date_to           = @DateTo,
                date_to_precision = @DateToPrecision,
                note              = @Note
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, new
        {
            fuzzyDate.Id,
            fuzzyDate.Precision,
            fuzzyDate.Date,
            fuzzyDate.DatePrecision,
            fuzzyDate.DateTo,
            fuzzyDate.DateToPrecision,
            fuzzyDate.Note
        }, transaction);
    }

    public async Task DeleteAsync(Guid id, IDbConnection connection, IDbTransaction transaction)
    {
        const string sql = "DELETE FROM public.fuzzy_dates WHERE id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id }, transaction);
    }
}
