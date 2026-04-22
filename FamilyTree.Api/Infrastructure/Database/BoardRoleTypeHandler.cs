using System.Data;
using Dapper;
using FamilyTreeApiV2.Shared;

namespace FamilyTreeApiV2.Infrastructure.Database;

public class BoardRoleTypeHandler : SqlMapper.TypeHandler<BoardRole>
{
    public override void SetValue(IDbDataParameter parameter, BoardRole value)
        => parameter.Value = value.ToString().ToLower();

    public override BoardRole Parse(object value)
        => Enum.Parse<BoardRole>((string)value, ignoreCase: true);
}
