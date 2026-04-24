using System.Net.Http.Headers;

namespace FamilyTreeApiV2.Infrastructure.Supabase;

public class SupabaseAdminService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    : ISupabaseAdminService
{
    private readonly string _supabaseUrl = configuration["Supabase:Url"]!;
    private readonly string _apiKey = configuration["Supabase:ApiKeySecret"]!;

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        using var http = httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Add("apikey", _apiKey);
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await http.DeleteAsync($"{_supabaseUrl}/auth/v1/admin/users/{userId}");

        return response.IsSuccessStatusCode;
    }
}
