using System.Text.Json.Serialization;

namespace MarketAnalystBot.Infrastructure.Brapi.Models;

public class BrapiQuoteResponse
{
    [JsonPropertyName("results")]
    public List<BrapiQuoteResult> Results { get; set; } = [];
}
