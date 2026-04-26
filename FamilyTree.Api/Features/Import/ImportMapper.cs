using FamilyTreeApiV2.Features.Persons;
using FamilyTreeApiV2.Shared;
using FamilyTreeApiV2.Shared.FuzzyDates;

namespace FamilyTreeApiV2.Features.Import;

internal static class ImportMapper
{
    internal static CreatePersonRequest ToCreatePersonRequest(V1Person v1) =>
        new(
            FirstName: v1.FirstName,
            LastName: v1.LastName,
            MiddleNames: v1.MiddleNames,
            BirthName: v1.BirthName,
            Gender: MapGender(v1.Gender),
            BirthPlace: v1.BirthPlace,
            BirthDate: v1.BirthDate is not null ? ToFuzzyDateRequest(v1.BirthDate) : null,
            DeathPlace: v1.DeathPlace,
            DeathDate: v1.DeathDate is not null ? ToFuzzyDateRequest(v1.DeathDate) : null,
            BurialPlace: v1.BurialPlace,
            Title: v1.Title,
            Religion: v1.Religion,
            Notes: v1.Notes);

    internal static FuzzyDateRequest ToFuzzyDateRequest(V1FuzzyDate v1) =>
        new(
            Precision: Enum.Parse<FuzzyDatePrecision>(v1.Precision, ignoreCase: true),
            Date: v1.Date is not null ? ParseDate(v1.Date) : null,
            DatePrecision: v1.DatePrecision is not null
                ? Enum.Parse<FuzzyDateFieldPrecision>(v1.DatePrecision, ignoreCase: true)
                : null,
            DateTo: v1.DateTo is not null ? ParseDate(v1.DateTo) : null,
            DateToPrecision: v1.DateToPrecision is not null
                ? Enum.Parse<FuzzyDateFieldPrecision>(v1.DateToPrecision, ignoreCase: true)
                : null,
            Note: v1.Note);

    private static DateOnly ParseDate(string value) =>
        DateOnly.FromDateTime(DateTimeOffset.Parse(value).Date);

    private static Gender? MapGender(string? gender) => gender switch
    {
        "male" => Gender.Male,
        "female" => Gender.Female,
        "diverse" => Gender.Diverse,
        _ => null
    };
}
