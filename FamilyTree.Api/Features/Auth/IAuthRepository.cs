namespace FamilyTreeApiV2.Features.Auth;

public interface IAuthRepository
{
    Task InsertUserAsync(string userId, string firstName, string lastName, string email);
    Task<(string FirstName, string LastName)?> GetUserNamesAsync(string userId);
}
