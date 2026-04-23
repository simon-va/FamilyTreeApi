using System.Net.Http.Headers;

namespace FamilyTreeApiV2.Infrastructure.Supabase;

public class SupabaseAdminService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    : ISupabaseAdminService
{
    public async Task<bool> DeleteUserAsync(string userId)
    {
        var supabaseUrl = configuration["Supabase:Url"];
        var apiKey = configuration["Supabase:ApiKeySecret"];

        using var http = httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Add("apikey", apiKey);
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await http.DeleteAsync($"{supabaseUrl}/auth/v1/admin/users/{userId}");

        return response.IsSuccessStatusCode;
    }
}
