namespace FamilyTreeApiV2.Shared.FuzzyDates;

public enum FuzzyDatePrecision
{
    Exact,
    Month,
    Year,
    Estimated,
    Before,
    After,
    Between
}

public enum FuzzyDateFieldPrecision
{
    Exact,
    Month,
    Year
}

public record FuzzyDate(
    Guid Id,
    FuzzyDatePrecision Precision,
    DateOnly Date,
    FuzzyDateFieldPrecision? DatePrecision,
    DateOnly? DateTo,
    FuzzyDateFieldPrecision? DateToPrecision,
    string? Note,
    DateTime CreatedAt);

public record FuzzyDateRequest(
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
