using System.ComponentModel.DataAnnotations;

namespace MarketAnalystBot.Web.Models
{
    public class OpportunityAnalysis
    {
        [Key]
        public long Id { get; set; }
        public string CodTicker { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Score { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public decimal? LastPrice { get; set; }
        public decimal? LastRsi { get; set; }
        public string PeriodsConfirmed { get; set; } = string.Empty; // e.g. "Daily;Weekly"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
