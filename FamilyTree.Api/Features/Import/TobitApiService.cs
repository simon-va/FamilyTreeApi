using System.Net.Http.Headers;
using System.Text.Json;

namespace FamilyTreeApiV2.Features.Import;

public class TobitApiService(IHttpClientFactory httpClientFactory) : ITobitApiService
{
    private const string BaseUrl = "https://run.chayns.codes/dd996dd6";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IEnumerable<V1Person>?> GetPersonsAsync(string token)
    {
        using var http = httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await http.GetAsync($"{BaseUrl}/persons");
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<IEnumerable<V1Person>>(JsonOptions);
    }
}
