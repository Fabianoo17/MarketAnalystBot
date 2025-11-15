using System.Text.Json.Serialization;

public class BrapiQuoteResult
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("regularMarketPrice")]
    public decimal RegularMarketPrice { get; set; }

    [JsonPropertyName("historicalDataPrice")]
    public List<HistoricalDataPrice> HistoricalDataPrice { get; set; } = [];
}
