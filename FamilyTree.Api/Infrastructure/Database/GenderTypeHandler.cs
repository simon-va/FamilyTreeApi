using System.Data;
using Dapper;
using FamilyTreeApiV2.Features.Persons;

namespace FamilyTreeApiV2.Infrastructure.Database;

public class GenderTypeHandler : SqlMapper.TypeHandler<Gender>
{
    public override void SetValue(IDbDataParameter parameter, Gender value)
        => parameter.Value = value.ToString().ToLower();

    public override Gender Parse(object value)
        => Enum.Parse<Gender>((string)value, ignoreCase: true);
}
