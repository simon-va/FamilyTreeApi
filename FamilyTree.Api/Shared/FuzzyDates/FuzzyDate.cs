namespace FamilyTreeApiV2.Shared.FuzzyDates;

public class FuzzyDate
{
    public Guid Id { get; set; }
    public FuzzyDatePrecision Precision { get; set; }
    public DateOnly Date { get; set; }
    public FuzzyDateFieldPrecision? DatePrecision { get; set; }
    public DateOnly? DateTo { get; set; }
    public FuzzyDateFieldPrecision? DateToPrecision { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

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
