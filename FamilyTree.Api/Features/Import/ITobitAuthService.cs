namespace FamilyTreeApiV2.Features.Import;

public interface ITobitAuthService
{
    Task<string?> GetTokenAsync(string username, string password);
}
