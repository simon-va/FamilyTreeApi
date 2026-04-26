using System.Data;
using Dapper;
using FamilyTreeApiV2.Features.Relations;

namespace FamilyTreeApiV2.Infrastructure.Database;

public class RelationTypeTypeHandler : SqlMapper.TypeHandler<RelationType>
{
    private static readonly Dictionary<RelationType, string> ToDb = new()
    {
        [RelationType.BiologicalParent] = "biological_parent",
        [RelationType.AdoptiveParent]   = "adoptive_parent",
        [RelationType.FosterParent]     = "foster_parent",
        [RelationType.Spouse]           = "spouse",
        [RelationType.Partner]          = "partner",
        [RelationType.Engaged]          = "engaged",
    };

    private static readonly Dictionary<string, RelationType> FromDb =
        ToDb.ToDictionary(x => x.Value, x => x.Key);

    public override void SetValue(IDbDataParameter parameter, RelationType value)
        => parameter.Value = ToDb[value];

    public override RelationType Parse(object value)
        => FromDb[(string)value];
}
