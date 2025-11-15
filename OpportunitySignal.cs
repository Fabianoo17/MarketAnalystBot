public class OpportunitySignal
{
    public string Ticker { get; set; } = string.Empty;
    public OpportunityType Type { get; set; } = OpportunityType.None;
    public double LastPrice { get; set; }
    public double LastRsi { get; set; }
    public DateTime Date { get; set; }   // <- NOVO: data do candle
    public string Reason { get; set; } = string.Empty;
}
