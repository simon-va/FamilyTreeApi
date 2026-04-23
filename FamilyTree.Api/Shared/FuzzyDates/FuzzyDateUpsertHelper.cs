using System.Data;
using ErrorOr;

namespace FamilyTreeApiV2.Shared.FuzzyDates;

internal static class FuzzyDateUpsertHelper
{
    internal static async Task<ErrorOr<FuzzyDate?>> UpsertAsync(
        IFuzzyDateRepository fuzzyDateRepository,
        FuzzyDateRequest? incoming,
        Guid? existingId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        if (incoming is not null)
        {
            if (existingId is null)
            {
                var newDate = new FuzzyDate(
                    Guid.NewGuid(),
                    incoming.Precision,
                    incoming.Date,
                    incoming.DatePrecision,
                    incoming.DateTo,
                    incoming.DateToPrecision,
                    incoming.Note,
                    default);
                await fuzzyDateRepository.CreateAsync(newDate, connection, transaction);
                return newDate;
            }
            else
            {
                var existing = await fuzzyDateRepository.GetByIdAsync(existingId.Value, connection, transaction);
                if (existing is null)
                    return Error.NotFound("FuzzyDate.NotFound", $"FuzzyDate {existingId} not found.");

                var updated = existing with
                {
                    Precision = incoming.Precision,
                    Date = incoming.Date,
                    DatePrecision = incoming.DatePrecision,
                    DateTo = incoming.DateTo,
                    DateToPrecision = incoming.DateToPrecision,
                    Note = incoming.Note
                };
                await fuzzyDateRepository.UpdateAsync(updated, connection, transaction);
                return updated;
            }
        }

        if (existingId is not null)
            await fuzzyDateRepository.DeleteAsync(existingId.Value, connection, transaction);

        return (FuzzyDate?)null;
    }
}
