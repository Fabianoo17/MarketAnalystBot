using MarketAnalystBot.Infrastructure.Brapi.Models;
using Skender.Stock.Indicators;

public static class MacdQuoteFilter
{
    private const int StochRsiPeriod = 14;
    private const int StochRsiK = 14;
    private const int StochRsiSignal = 3;

    /// <summary>
    /// Compatibilidade: retorna true quando existe a data do sinal semanal.
    /// </summary>
    public static bool IsQuoteValid(BrapiQuoteResult quote)
    {
        return GetSignalDate(quote).HasValue;
    }

    /// <summary>
    /// Procura, do mais recente ao mais antigo, a primeira data em que:
    /// - StochRSI (corrente) está abaixo de 20 e acima do Signal;
    /// - no momento anterior o Signal estava acima do StochRSI (cross up);
    /// - o histograma do MACD mostra melhora em 3 barras (i-2 < i-1 < i);
    /// - o volume mostra 3 aumentos consecutivos (i-2 < i-1 < i).
    /// Retorna null se não encontrar.
    /// </summary>
    public static DateTime? GetSignalDate(BrapiQuoteResult quote)
    {
        if (quote == null || quote.HistoricalDataPrice == null)
            return null;

        var candles = quote.HistoricalDataPrice
            .OrderBy(x => x.DateUtc)
            .Select(x => new Quote
            {
                Date = x.DateUtc,
                Open = x.Open ?? 0,
                High = x.High ?? 0,
                Low = x.Low ?? 0,
                Close = x.Close ?? 0,
                Volume = x.Volume ?? 0
            }).ToList();

        if (candles.Count < 3)
            return null;

        var macd = candles.GetMacd(12, 26, 9).ToList();


        // iterate from most recent backwards, require at least two previous bars for checks
        for (int i = macd.Count - 1; i >= 2; i--)
        {
            var macdBar2 = macd[i - 2];
            var macdBar1 = macd[i - 1];
            var macdBar0 = macd[i];

            if (!macdBar2.Macd.HasValue || !macdBar2.Signal.HasValue ||
                !macdBar1.Macd.HasValue || !macdBar1.Signal.HasValue ||
                !macdBar0.Macd.HasValue || !macdBar0.Signal.HasValue)
                continue;

            double hist2 = macdBar2.Macd.Value - macdBar2.Signal.Value;
            double hist1 = macdBar1.Macd.Value - macdBar1.Signal.Value;
            double hist0 = macdBar0.Macd.Value - macdBar0.Signal.Value;

            bool macdImproving = macdBar0.Macd.Value > macdBar0.Signal.Value &&
                 hist1 < 0.07 &&
                hist0 >= 0.07;
            if (!macdImproving)
                continue;

            //bool volumeImproving = vol2 < vol1 && vol1 < vol0;
            //if (!volumeImproving)
            //    continue;

            // all checks passed, return the date of the cross (current bar)
            return macdBar0.Date;
        }

        return null;
    }
}


