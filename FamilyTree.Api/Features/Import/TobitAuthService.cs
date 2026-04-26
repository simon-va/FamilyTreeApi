using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FamilyTreeApiV2.Features.Import;

public class TobitAuthService(IHttpClientFactory httpClientFactory) : ITobitAuthService
{
    private const string TokenUrl = "https://auth.tobit.com/v2/token";
    private const int LocationId = 243012;

    public async Task<string?> GetTokenAsync(string username, string password)
    {
        using var http = httpClientFactory.CreateClient();

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var body = JsonSerializer.Serialize(new { tokenType = 1, locationId = LocationId });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await http.PostAsync(TokenUrl, content);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("token", out var tokenProp) ? tokenProp.GetString() : null;
    }
}
