using System.Text.Json.Serialization;

public class BrapiQuoteResponse
{
    [JsonPropertyName("results")]
    public List<BrapiQuoteResult> Results { get; set; } = [];
}
