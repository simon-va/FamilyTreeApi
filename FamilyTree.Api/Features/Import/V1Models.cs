namespace FamilyTreeApiV2.Features.Import;

public record V1Person(
    string Id,
    string FirstName,
    string LastName,
    string? MiddleNames,
    string? BirthName,
    string? Gender,
    string? BirthPlace,
    V1FuzzyDate? BirthDate,
    string? DeathPlace,
    V1FuzzyDate? DeathDate,
    string? BurialPlace,
    string? Title,
    string? Religion,
    string? Notes);

public record V1FuzzyDate(
    string Id,
    string Precision,
    string? Date,
    string? DatePrecision,
    string? DateTo,
    string? DateToPrecision,
    string? Note);

public record V1Relation(
    string Id,
    string PersonAId,
    string PersonBId,
    string Type,
    V1FuzzyDate? StartDate,
    V1FuzzyDate? EndDate,
    string? EndReason,
    string Notes);

public record V1Residence(
    string Id,
    string PersonId,
    string? City,
    string? Country,
    string? Street,
    string? Notes,
    V1FuzzyDate? StartDate,
    V1FuzzyDate? EndDate);
