using System.Data;
using Dapper;
using FamilyTreeApiV2.Shared.FuzzyDates;

namespace FamilyTreeApiV2.Infrastructure.Database;

public class FuzzyDateFieldPrecisionTypeHandler : SqlMapper.TypeHandler<FuzzyDateFieldPrecision>
{
    public override void SetValue(IDbDataParameter parameter, FuzzyDateFieldPrecision value)
        => parameter.Value = value.ToString().ToLower();

    public override FuzzyDateFieldPrecision Parse(object value)
        => Enum.Parse<FuzzyDateFieldPrecision>((string)value, ignoreCase: true);
}
