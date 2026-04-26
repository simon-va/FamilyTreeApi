namespace FamilyTreeApiV2.Features.Import;

public interface ITobitApiService
{
    Task<IEnumerable<V1Person>?> GetPersonsAsync(string token);
    Task<IEnumerable<V1Relation>?> GetRelationsAsync(string token);
}
