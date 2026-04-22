using System.Data;
using ErrorOr;
using FamilyTreeApiV2.Features.Persons;

namespace FamilyTreeApiV2.Shared.FuzzyDates;

internal static class FuzzyDateUpsertHelper
{
    internal static async Task<ErrorOr<FuzzyDate?>> UpsertAsync(
        IFuzzyDateRepository fuzzyDateRepository,
        FuzzyDateInputDto? incoming,
        Guid? existingId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        if (incoming is not null)
        {
            if (existingId is null)
            {
                var newDate = new FuzzyDate
                {
                    Id = Guid.NewGuid(),
                    Precision = incoming.Precision,
                    Date = incoming.Date,
                    DatePrecision = incoming.DatePrecision,
                    DateTo = incoming.DateTo,
                    DateToPrecision = incoming.DateToPrecision,
                    Note = incoming.Note
                };
                await fuzzyDateRepository.CreateAsync(newDate, connection, transaction);
                return newDate;
            }
            else
            {
                var existing = await fuzzyDateRepository.GetByIdAsync(existingId.Value, connection, transaction);
                if (existing is null)
                    return Error.NotFound("FuzzyDate.NotFound", $"FuzzyDate {existingId} not found.");

                existing.Precision = incoming.Precision;
                existing.Date = incoming.Date;
                existing.DatePrecision = incoming.DatePrecision;
                existing.DateTo = incoming.DateTo;
                existing.DateToPrecision = incoming.DateToPrecision;
                existing.Note = incoming.Note;

                await fuzzyDateRepository.UpdateAsync(existing, connection, transaction);
                return existing;
            }
        }

        if (existingId is not null)
            await fuzzyDateRepository.DeleteAsync(existingId.Value, connection, transaction);

        return (FuzzyDate?)null;
    }
}
