namespace FamilyTreeApiV2.Features.Auth;

public interface IAuthRepository
{
    Task InsertUserAsync(Guid userId, string firstName, string lastName, string email);
    Task<(string FirstName, string LastName)?> GetUserNamesAsync(Guid userId);
    Task<bool> IsLastOwnerOfAnyBoardAsync(Guid userId);
    Task DeleteUserAsync(Guid userId);
}
