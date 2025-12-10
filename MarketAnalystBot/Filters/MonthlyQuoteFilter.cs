using MarketAnalystBot.Infrastructure.Brapi.Models;
using Skender.Stock.Indicators;

public static class MonthlyQuoteFilter
{
    private const int RsiPeriod = 14;
    private const int StochPeriod = 14;
    private const int StochSignalPeriod = 3;
    private const double RsiThreshold = 20f;

    public static bool IsQuoteValid(BrapiQuoteResult quote)
    {
        // keep compatibility: return true when a cross date exists
        return GetStochRsiCrossDate(quote).HasValue;
    }

    /// <summary>
    /// Percorre do ponto mais recente para o mais antigo e retorna a data
    /// em que o StochRSI (abaixo de 20) cruzou para cima do Signal.
    /// Condição definida como: no momento corrente StochRsi &lt; 20 e StochRsi &gt; Signal
    /// e no momento anterior Signal &gt; StochRsi.
    /// Retorna null se não encontrar.
    /// </summary>
    public static DateTime? GetStochRsiCrossDate(BrapiQuoteResult quote)
    {
        if (quote == null || quote.HistoricalDataPrice == null)
            return null;

        var monthlyCandles = quote.HistoricalDataPrice
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

        var stochRsiResults = monthlyCandles.GetStochRsi(14, 14, 3, 3).ToList();

        if (stochRsiResults.Count < 2)
            return null;

        const double threshold = 20.0;

        for (int i = stochRsiResults.Count - 1; i >= 1; i--)
        {
            var prev = stochRsiResults[i - 1];
            var curr = stochRsiResults[i];

            if (!prev.StochRsi.HasValue || !prev.Signal.HasValue ||
                !curr.StochRsi.HasValue || !curr.Signal.HasValue)
                continue;

            double prevStoch = prev.StochRsi.Value;
            double currStoch = curr.StochRsi.Value;
            double prevSignal = prev.Signal.Value;
            double currSignal = curr.Signal.Value;

            // current: StochRSI below threshold and above signal
            // previous: signal above StochRSI (was above -> this is a cross up)
            if (currStoch < threshold && currStoch >= currSignal && prevSignal > prevStoch)
            {
                return curr.Date;
            }
        }

        return null;
    }
}