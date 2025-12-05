using System.Text.Json;
using MarketAnalystBot.Application.Contracts;
using MarketAnalystBot.Infrastructure.Brapi.Models;

namespace MarketAnalystBot.Infrastructure.Brapi;

public class BrapiClient : IBrapiClient
{
    private readonly HttpClient _httpClient;
    private readonly BrapiSettings _settings;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BrapiClient(BrapiSettings settings)
    {
        _settings = settings;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(settings.BaseUrl)
        };

        if (!string.IsNullOrWhiteSpace(settings.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.Token);
        }
    }

    public async Task<BrapiQuoteResult?> GetDailyHistoryAsync(string ticker, string range = "3mo", string interval = "1d")
    {
        var url = $"/api/quote/{ticker}?range={range}&interval={interval}&token={_settings.Token}";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Erro ao chamar brapi: {(int)response.StatusCode} - {response.ReasonPhrase}");
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        var data = await JsonSerializer.DeserializeAsync<BrapiQuoteResponse>(stream, JsonOptions);

        return data?.Results.FirstOrDefault();
    }

    public async Task<QuoteListResponse?> GetTickersList( string range = "3mo", string interval = "1d")
    {
        var url = $"/api/quote/list?token={_settings.Token}";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Erro ao chamar brapi: {(int)response.StatusCode} - {response.ReasonPhrase}");
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        var data = await JsonSerializer.DeserializeAsync<QuoteListResponse>(stream, JsonOptions);

        return data;
    }
}
