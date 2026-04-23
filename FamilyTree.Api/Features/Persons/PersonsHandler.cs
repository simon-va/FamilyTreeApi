using ErrorOr;
using FamilyTreeApiV2.Infrastructure.Database;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;

namespace FamilyTreeApiV2.Features.Persons;

public class PersonsHandler(
    IPersonsRepository repository,
    IFuzzyDateRepository fuzzyDateRepository,
    IDbConnectionFactory connectionFactory)
{
    public async Task<ErrorOr<List<PersonResponse>>> GetAllAsync(Guid boardId, Guid userId)
    {
        var role = await repository.GetCallerRoleAsync(boardId, userId);
        if (role is null)
            return PersonsErrors.BoardNotFound;

        var persons = await repository.GetAllAsync(boardId);
        return persons.Select(p => ToResponse(p.Person, p.BirthDate, p.DeathDate)).ToList();
    }

    public async Task<ErrorOr<PersonResponse>> CreateAsync(Guid boardId, CreatePersonRequest request, Guid userId)
    {
        var role = await repository.GetCallerRoleAsync(boardId, userId);
        if (role is null)
            return PersonsErrors.BoardNotFound;
        if (role is BoardRole.Viewer)
            return PersonsErrors.Forbidden;

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var birthResult = await FuzzyDateUpsertHelper.UpsertAsync(
            fuzzyDateRepository, request.BirthDate, null, connection, transaction);
        if (birthResult.IsError) return birthResult.FirstError;

        var deathResult = await FuzzyDateUpsertHelper.UpsertAsync(
            fuzzyDateRepository, request.DeathDate, null, connection, transaction);
        if (deathResult.IsError) return deathResult.FirstError;

        var person = await repository.CreateAsync(
            boardId, request, birthResult.Value?.Id, deathResult.Value?.Id, connection, transaction);

        transaction.Commit();

        return ToResponse(person, birthResult.Value, deathResult.Value);
    }

    public async Task<ErrorOr<PersonResponse>> UpdateAsync(
        Guid boardId, Guid personId, UpdatePersonRequest request, Guid userId)
    {
        var role = await repository.GetCallerRoleAsync(boardId, userId);
        if (role is null)
            return PersonsErrors.BoardNotFound;
        if (role is BoardRole.Viewer)
            return PersonsErrors.Forbidden;

        var existing = await repository.GetByIdAsync(boardId, personId);
        if (existing is null)
            return PersonsErrors.PersonNotFound;

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var birthResult = await FuzzyDateUpsertHelper.UpsertAsync(
            fuzzyDateRepository, request.BirthDate, existing.BirthDateId, connection, transaction);
        if (birthResult.IsError) return birthResult.FirstError;

        var deathResult = await FuzzyDateUpsertHelper.UpsertAsync(
            fuzzyDateRepository, request.DeathDate, existing.DeathDateId, connection, transaction);
        if (deathResult.IsError) return deathResult.FirstError;

        var person = await repository.UpdateAsync(
            boardId, personId, request, birthResult.Value?.Id, deathResult.Value?.Id, connection, transaction);
        if (person is null)
            return PersonsErrors.PersonNotFound;

        transaction.Commit();

        return ToResponse(person, birthResult.Value, deathResult.Value);
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(Guid boardId, Guid personId, Guid userId)
    {
        var role = await repository.GetCallerRoleAsync(boardId, userId);
        if (role is null)
            return PersonsErrors.BoardNotFound;
        if (role is BoardRole.Viewer)
            return PersonsErrors.Forbidden;

        var deleted = await repository.DeleteAsync(boardId, personId);
        if (!deleted)
            return PersonsErrors.PersonNotFound;

        return Result.Deleted;
    }

    private static PersonResponse ToResponse(PersonRow person, FuzzyDate? birthDate, FuzzyDate? deathDate) =>
        new(person.Id, person.BoardId, person.FirstName, person.LastName, person.MiddleNames,
            person.BirthName, person.Gender, person.BirthPlace, ToFuzzyDateResponse(birthDate),
            person.DeathPlace, ToFuzzyDateResponse(deathDate), person.BurialPlace,
            person.Title, person.Religion, person.Notes, person.CreatedAt);

    private static FuzzyDateResponse? ToFuzzyDateResponse(FuzzyDate? fuzzyDate) =>
        fuzzyDate is null ? null
            : new(fuzzyDate.Id, fuzzyDate.Precision, fuzzyDate.Date, fuzzyDate.DatePrecision,
                  fuzzyDate.DateTo, fuzzyDate.DateToPrecision, fuzzyDate.Note);
}
