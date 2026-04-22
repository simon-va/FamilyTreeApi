using System.Data;

namespace FamilyTreeApiV2.Shared.FuzzyDates;

public interface IFuzzyDateRepository
{
    Task CreateAsync(FuzzyDate fuzzyDate, IDbConnection connection, IDbTransaction transaction);
    Task<FuzzyDate?> GetByIdAsync(Guid id, IDbConnection connection, IDbTransaction transaction);
    Task UpdateAsync(FuzzyDate fuzzyDate, IDbConnection connection, IDbTransaction transaction);
    Task DeleteAsync(Guid id, IDbConnection connection, IDbTransaction transaction);
}
