namespace FamilyTreeApiV2.Infrastructure.Supabase;

public interface ISupabaseAdminService
{
    Task<bool> DeleteUserAsync(Guid userId);
}
