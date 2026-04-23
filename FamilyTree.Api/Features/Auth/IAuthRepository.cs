namespace FamilyTreeApiV2.Features.Auth;

public interface IAuthRepository
{
    Task<User> InsertUserAsync(Guid userId, string firstName, string lastName, string email);
    Task<User?> GetUserAsync(Guid userId);
    Task<bool> IsLastOwnerOfAnyBoardAsync(Guid userId);
    Task DeleteUserAsync(Guid userId);
}
