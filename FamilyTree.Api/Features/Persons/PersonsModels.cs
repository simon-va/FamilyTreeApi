using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;

namespace FamilyTreeApiV2.Features.Persons;

public record CreatePersonRequest(
    string FirstName,
    string LastName,
    string? MiddleNames,
    string? BirthName,
    Gender? Gender,
    string? BirthPlace,
    FuzzyDateRequest? BirthDate,
    string? DeathPlace,
    FuzzyDateRequest? DeathDate,
    string? BurialPlace,
    string? Title,
    string? Religion,
    string? Notes);

public record UpdatePersonRequest(
    string FirstName,
    string LastName,
    string? MiddleNames,
    string? BirthName,
    Gender? Gender,
    string? BirthPlace,
    FuzzyDateRequest? BirthDate,
    string? DeathPlace,
    FuzzyDateRequest? DeathDate,
    string? BurialPlace,
    string? Title,
    string? Religion,
    string? Notes);

public record Person(
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

public record PersonResponse(
    Guid Id,
    Guid BoardId,
    string FirstName,
    string LastName,
    string? MiddleNames,
    string? BirthName,
    Gender? Gender,
    string? BirthPlace,
    FuzzyDateResponse? BirthDate,
    string? DeathPlace,
    FuzzyDateResponse? DeathDate,
    string? BurialPlace,
    string? Title,
    string? Religion,
    string? Notes,
    DateTime CreatedAt);
