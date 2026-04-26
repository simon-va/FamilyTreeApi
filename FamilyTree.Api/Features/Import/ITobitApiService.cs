namespace FamilyTreeApiV2.Features.Import;

public interface ITobitApiService
{
    Task<IEnumerable<V1Person>?> GetPersonsAsync(string token);
}
