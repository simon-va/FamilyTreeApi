using System.Data;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;

namespace FamilyTreeApiV2.Features.Persons;

public interface IPersonsRepository
{
    Task<BoardRole?> GetCallerRoleAsync(Guid boardId, Guid userId);
    Task<IEnumerable<(PersonRow Person, FuzzyDate? BirthDate, FuzzyDate? DeathDate)>> GetAllAsync(Guid boardId);
    Task<PersonRow?> GetByIdAsync(Guid boardId, Guid personId);
    Task<PersonRow> CreateAsync(Guid boardId, CreatePersonRequest request, Guid? birthDateId, Guid? deathDateId, IDbConnection connection, IDbTransaction transaction);
    Task<PersonRow?> UpdateAsync(Guid boardId, Guid personId, UpdatePersonRequest request, Guid? birthDateId, Guid? deathDateId, IDbConnection connection, IDbTransaction transaction);
    Task<bool> DeleteAsync(Guid boardId, Guid personId);
}

public record PersonRow(
    Guid Id,
    Guid BoardId,
    string FirstName,
    string LastName,
    string? MiddleNames,
    string? BirthName,
    Gender? Gender,
    string? BirthPlace,
    string? DeathPlace,
    string? BurialPlace,
    string? Title,
    string? Religion,
    string? Notes,
    DateTime CreatedAt,
    Guid? BirthDateId,
    Guid? DeathDateId);
