using System.Globalization;
using MarketAnalystBot.Application.Contracts;
using MarketAnalystBot.Application.Services;
using MarketAnalystBot.Domain.Entities;
using MarketAnalystBot.Infrastructure.Brapi;


internal class Program
{
    private static readonly List<string> Tickers =
    [
        "ABEV3", "ALOS3", "ASAI3", "AURE3", "AXIA3", "AXIA6", "AZUL4", "AZZA3", "B3SA3", "BBAS3", "BBDC3", "BBDC4",
        "BBSE3", "BEEF3", "BRAP4", "BRAV3", "BRKM5", "CMIG4", "CMIN3", "COGN3", "CPFE3", "CSAN3",
        "CSNA3", "CVCB3", "CXSE3", "CYRE3", "DIRR3", "EGIE3", "EMBJ3", "ENEV3", "ENGI1", "EQTL3", "FLRY3", "GGBR4",
        "GOAU4", "HAPV3", "HYPE3", "IGTI1", "IRBR3", "ISAE4", "ITSA4", "ITUB4", "KLBN1", "LREN3", "MBRF3", "MGLU3",
        "MOTV3", "MRVE3", "MULT3", "NATU3", "PCAR3", "PETR3", "PETR4", "PETZ3", "POMO4", "PRIO3", "PSSA3", "RADL3",
        "RAIL3", "RAIZ4", "RDOR3", "RECV3", "RENT3", "SANB1", "SBSP3", "SLCE3", "SMFT3", "SMTO3", "STBP3", "SUZB3",
        "TAEE1", "TIMS3", "TOTS3", "UGPA3", "USIM5", "VALE3", "VAMO3", "VBBR3", "VIVA3", "VIVT3", "WEGE3", "YDUQ3"
    ];

    private async static Task Main(string[] args)
    {
        string? token = "gE9YjNgLoWdfqSZUHNMtot";

        var settings = new BrapiSettings
        {
            Token = string.IsNullOrWhiteSpace(token) ? null : token
        };

        IBrapiClient client = new BrapiClient(settings);
        IOpportunityEngine engine = new OpportunityEngine();

        foreach (var ticker in Tickers)
        {
            Console.WriteLine("=== OptionsOpportunityAnalyzer ===");
            Console.WriteLine($"Ticker: {ticker}");
            Console.WriteLine(token is null
                ? "⚠️ Sem token BRAPI_TOKEN. Usando apenas ações liberadas públicas da brapi."
                : "Token BRAPI_TOKEN detectado. Usando brapi.dev Pro.");

            try
            {
                var quote = await client.GetDailyHistoryAsync(ticker, range: "6mo", interval: "1d");

                if (quote is null || quote.HistoricalDataPrice is null || quote.HistoricalDataPrice.Count == 0)
                {
                    Console.WriteLine("Não foi possível obter dados da brapi.dev.");
                    continue;
                }

                var lastSignal = engine.Analyze(quote);
                //if (lastSignal.Type == OpportunityType.None) continue;
                Console.WriteLine();
                Console.WriteLine("=== SINAL MAIS RECENTE ===");
                Console.WriteLine($"Data:   {lastSignal.Date:yyyy-MM-dd}");
                Console.WriteLine($"Preço:  {lastSignal.LastPrice.ToString("F2", CultureInfo.InvariantCulture)}");
                Console.WriteLine($"RSI(14): {lastSignal.LastRsi:F2}");
                Console.WriteLine($"Tipo:   {lastSignal.Type}");
                Console.WriteLine($"Motivo: {lastSignal.Reason}");

                Console.WriteLine();
                Console.WriteLine("=== SINAIS HISTÓRICOS (BACKTEST SIMPLES) ===");

                var historySignals = engine.AnalyzeHistory(quote);

                if (!historySignals.Any())
                {
                    Console.WriteLine("Nenhum sinal forte encontrado no período analisado.");
                }
                else
                {
                    foreach (var s in historySignals.OrderBy(s => s.Date))
                    {
                        Console.WriteLine("----------------------------------------");
                        Console.WriteLine($"Data:   {s.Date:yyyy-MM-dd}");
                        Console.WriteLine($"Tipo:   {s.Type}");
                        Console.WriteLine($"Preço:  {s.LastPrice.ToString("F2", CultureInfo.InvariantCulture)}");
                        Console.WriteLine($"RSI(14): {s.LastRsi:F2}");
                        Console.WriteLine($"Motivo: {s.Reason}");
                    }

                    Console.WriteLine("----------------------------------------");
                    Console.WriteLine($"Total de sinais encontrados: {historySignals.Count}");
                }

                Console.WriteLine();
                Console.WriteLine("Dica: pegue essas datas e jogue no seu gráfico (TradingView, Profit, etc.) pra validar se o sinal fazia sentido.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro inesperado:");
                Console.WriteLine(ex);
            }
        }
    }
}
