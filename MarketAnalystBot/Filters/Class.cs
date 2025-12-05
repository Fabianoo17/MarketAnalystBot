using MarketAnalystBot.Infrastructure.Brapi.Models;
using Skender.Stock.Indicators;

public static class WatchlistFilter
{
    private const decimal MinVolumeMoney = 5_000_000m;
    private const decimal MinPrice = 5m;
    private const int MinHistoryDays = 250;
    private const decimal MinAtrPct = 0.01m;
    private const decimal MaxAtrPct = 0.08m;

    public static bool TryEvaluate(BrapiQuoteResult quote, out decimal score)
    {
        score = 0;
        if (quote == null) { Console.WriteLine("Quote não obtido"); return false; };
        var candles = quote.HistoricalDataPrice
            .OrderBy(x => x.DateUtc)
            .Select(x => new Quote
            {
                Date = x.DateUtc,
                Close = x.Close ?? 0,
                High = x.High ?? 0,
                Low = x.Low ?? 0,
                Open = x.Open ?? 0,
                Volume = x.Volume ?? 0
            }).ToList();

        if (candles.Count < MinHistoryDays)
            return false;

        decimal lastPrice = (decimal)candles.Last().Close;

        if (lastPrice < MinPrice)
            return false;

        // 📌 Volume financeiro médio – últimos 20 dias
        var last20 = candles.Skip(Math.Max(0, candles.Count - 20)).ToList();
        decimal volumeMoneyAvg = last20.Average(c => (decimal)c.Close * c.Volume);

        if (volumeMoneyAvg < MinVolumeMoney)
            return false;

        // 📌 ATR% para medir volatilidade
        var atrResult = candles.GetAtr(14).ToList();
        var lastAtr = atrResult.LastOrDefault()?.Atr;
        if (lastAtr == null)
            return false;

        decimal atrPct = (decimal)lastAtr / lastPrice;
        if (quote.Symbol == "PETR3")
            Console.WriteLine("PETR3 Teste");


        // ⭐ Score: quanto maior liquidez + melhor volatilidade, melhor
        score =
            Normalize(volumeMoneyAvg, 10_000_000, 50_000_000) * 0.6m + // peso 60%
            Normalize(atrPct, MinAtrPct, MaxAtrPct) * 0.4m;           // peso 40%

        return true;
    }

    // Normalização simples para 0–1
    private static decimal Normalize(decimal value, decimal min, decimal max)
        => (value - min) / (max - min) < 0
        ? 0
        : Math.Min(1, (value - min) / (max - min));
}



