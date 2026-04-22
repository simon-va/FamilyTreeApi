using System.Data;
using Dapper;
using FamilyTreeApiV2.Shared.FuzzyDates;

namespace FamilyTreeApiV2.Infrastructure.Database;

public class FuzzyDatePrecisionTypeHandler : SqlMapper.TypeHandler<FuzzyDatePrecision>
{
    public override void SetValue(IDbDataParameter parameter, FuzzyDatePrecision value)
        => parameter.Value = value.ToString().ToLower();

    public override FuzzyDatePrecision Parse(object value)
        => Enum.Parse<FuzzyDatePrecision>((string)value, ignoreCase: true);
}
