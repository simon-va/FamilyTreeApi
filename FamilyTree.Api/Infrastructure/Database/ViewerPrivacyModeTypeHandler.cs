using System.Data;
using Dapper;
using FamilyTreeApiV2.Shared;

namespace FamilyTreeApiV2.Infrastructure.Database;

public class ViewerPrivacyModeTypeHandler : SqlMapper.TypeHandler<ViewerPrivacyMode>
{
    public override void SetValue(IDbDataParameter parameter, ViewerPrivacyMode value)
        => parameter.Value = value.ToString().ToLower();

    public override ViewerPrivacyMode Parse(object value)
        => Enum.Parse<ViewerPrivacyMode>((string)value, ignoreCase: true);
}

public class NullableViewerPrivacyModeTypeHandler : SqlMapper.TypeHandler<ViewerPrivacyMode?>
{
    public override void SetValue(IDbDataParameter parameter, ViewerPrivacyMode? value)
        => parameter.Value = value.HasValue ? (object)value.Value.ToString().ToLower() : DBNull.Value;

    public override ViewerPrivacyMode? Parse(object value)
        => value is null or DBNull ? null : Enum.Parse<ViewerPrivacyMode>((string)value, ignoreCase: true);
}
