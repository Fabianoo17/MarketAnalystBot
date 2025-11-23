using System.Text.Json.Serialization;

namespace MarketAnalystBot.Infrastructure.Brapi.Models;

public class HistoricalDataPrice
{
    [JsonPropertyName("date")]
    public long Date { get; set; }

    [JsonPropertyName("close")]
    public decimal Close { get; set; }

    [JsonPropertyName("open")]
    public decimal Open { get; set; }

    [JsonPropertyName("high")]
    public decimal High { get; set; }

    [JsonPropertyName("low")]
    public decimal Low { get; set; }

    [JsonPropertyName("volume")]
    public long Volume { get; set; }

    [JsonPropertyName("adjustedClose")]
    public decimal AdjustedClose { get; set; }

    [JsonIgnore]
    public DateTime DateUtc => DateTimeOffset.FromUnixTimeSeconds(Date).UtcDateTime;
}
