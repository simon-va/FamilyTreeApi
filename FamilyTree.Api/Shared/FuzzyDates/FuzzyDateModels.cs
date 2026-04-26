namespace FamilyTreeApiV2.Shared.FuzzyDates;

public enum FuzzyDatePrecision
{
    Exact = 0,
    Month = 1,
    Year = 2,
    Estimated = 3,
    Before = 4,
    After = 5,
    Between = 6,
    Unknown = 7
}

public enum FuzzyDateFieldPrecision
{
    Exact = 0,
    Month = 1,
    Year = 2
}

public record FuzzyDate(
    Guid Id,
    FuzzyDatePrecision Precision,
    DateOnly? Date,
    FuzzyDateFieldPrecision? DatePrecision,
    DateOnly? DateTo,
    FuzzyDateFieldPrecision? DateToPrecision,
    string? Note,
    DateTime CreatedAt);

public record FuzzyDateRequest(
    FuzzyDatePrecision Precision,
    DateOnly? Date,
    FuzzyDateFieldPrecision? DatePrecision,
    DateOnly? DateTo,
    FuzzyDateFieldPrecision? DateToPrecision,
    string? Note);

public record FuzzyDateResponse(
    Guid Id,
    FuzzyDatePrecision Precision,
    DateOnly? Date,
    FuzzyDateFieldPrecision? DatePrecision,
    DateOnly? DateTo,
    FuzzyDateFieldPrecision? DateToPrecision,
    string? Note);
