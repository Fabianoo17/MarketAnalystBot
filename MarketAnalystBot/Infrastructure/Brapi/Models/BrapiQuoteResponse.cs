using System.Text.Json.Serialization;

namespace MarketAnalystBot.Infrastructure.Brapi.Models;

public class BrapiQuoteResponse
{
    [JsonPropertyName("results")]
    public List<BrapiQuoteResult> Results { get; set; } = [];
}


public class QuoteListResponse
{
    [JsonPropertyName("indexes")]
    public List<IndexInfo>? Indexes { get; set; }

    [JsonPropertyName("stocks")]
    public List<StockSummary>? Stocks { get; set; }
}

public class IndexInfo
{
    [JsonPropertyName("stock")]
    public string? Stock { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class StockSummary
{
    [JsonPropertyName("stock")]
    public string? Stock { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("close")]
    public decimal? Close { get; set; }

    [JsonPropertyName("change")]
    public decimal? Change { get; set; }

    [JsonPropertyName("volume")]
    public long Volume { get; set; }

    [JsonPropertyName("market_cap")]
    public decimal? MarketCap { get; set; }

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }

    [JsonPropertyName("sector")]
    public string? Sector { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }   // "stock", "fund", etc.
}
