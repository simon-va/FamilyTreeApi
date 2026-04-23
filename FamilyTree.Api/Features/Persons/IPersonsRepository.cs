using System.Data;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;

namespace FamilyTreeApiV2.Features.Persons;

public interface IPersonsRepository
{
    Task<BoardRole?> GetCallerRoleAsync(Guid boardId, Guid userId);
    Task<IEnumerable<(Person Person, FuzzyDate? BirthDate, FuzzyDate? DeathDate)>> GetAllAsync(Guid boardId);
    Task<Person?> GetByIdAsync(Guid boardId, Guid personId);
    Task<Person> CreateAsync(Guid boardId, CreatePersonRequest request, Guid? birthDateId, Guid? deathDateId, IDbConnection connection, IDbTransaction transaction);
    Task<Person?> UpdateAsync(Guid boardId, Guid personId, UpdatePersonRequest request, Guid? birthDateId, Guid? deathDateId, IDbConnection connection, IDbTransaction transaction);
    Task<bool> DeleteAsync(Guid boardId, Guid personId);
}
