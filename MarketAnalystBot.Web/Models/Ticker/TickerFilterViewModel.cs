namespace MarketAnalystBot.Web.Models.Ticker
{
    public class TickerFilterViewModel
    {
        // Filtros
        public string? CodTicker { get; set; }
        public string? Sector { get; set; }
        public decimal? MinScore { get; set; }
        public decimal? MaxScore { get; set; }

        // Opções de setor para o dropdown
        public List<string>? AvailableSectors { get; set; } = new();

        // Resultado da busca
        public IEnumerable<Tickers>? Resultados { get; set; } = new List<Tickers>();
    }

}
