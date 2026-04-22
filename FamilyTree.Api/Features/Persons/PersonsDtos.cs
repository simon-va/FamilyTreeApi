using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;

namespace FamilyTreeApiV2.Features.Persons;

public record FuzzyDateInputDto(
    FuzzyDatePrecision Precision,
    DateOnly Date,
    FuzzyDateFieldPrecision? DatePrecision,
    DateOnly? DateTo,
    FuzzyDateFieldPrecision? DateToPrecision,
    string? Note);

public record FuzzyDateResponse(
    Guid Id,
    FuzzyDatePrecision Precision,
    DateOnly Date,
    FuzzyDateFieldPrecision? DatePrecision,
    DateOnly? DateTo,
    FuzzyDateFieldPrecision? DateToPrecision,
    string? Note);

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

public record CreatePersonRequest(
    string FirstName,
    string LastName,
    string? MiddleNames,
    string? BirthName,
    Gender? Gender,
    string? BirthPlace,
    FuzzyDateInputDto? BirthDate,
    string? DeathPlace,
    FuzzyDateInputDto? DeathDate,
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
    FuzzyDateInputDto? BirthDate,
    string? DeathPlace,
    FuzzyDateInputDto? DeathDate,
    string? BurialPlace,
    string? Title,
    string? Religion,
    string? Notes);
