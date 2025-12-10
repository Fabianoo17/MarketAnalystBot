using MarketAnalystBot.Domain.Entities;
using MarketAnalystBot.Infrastructure.Brapi.Models;
using Skender.Stock.Indicators;

public class MonthlyOpportunityEngine
{
    private const int FastEmaPeriod = 9;   // EMA curta (9 meses)
    private const int SlowEmaPeriod = 21;  // EMA longa (21 meses)
    private const int RsiPeriod = 14;

    public List<OpportunitySignal> AnalyzeMonthlyHistory(BrapiQuoteResult quote, string ticker)
    {
        var opportunities = new List<OpportunitySignal>();

        // 1) Monta candles a partir do resultado da Brapi
        var candles = quote.HistoricalDataPrice
            .Select(x => new Quote
            {
                Date   = x.DateUtc, // ou x.Date
                Open   = (decimal)x.Open,
                High   = (decimal)x.High,
                Low    = (decimal)x.Low,
                Close  = (decimal)x.Close,
                Volume = x.Volume.HasValue ? (decimal)x.Volume.Value : 0m
            })
            .OrderBy(c => c.Date)
            .ToList();

        if (candles.Count < SlowEmaPeriod + 5)
            return opportunities; // histórico insuficiente, retorna lista vazia

        // 2) Calcula indicadores
        var emaFastList = candles.GetEma(FastEmaPeriod).ToList();
        var emaSlowList = candles.GetEma(SlowEmaPeriod).ToList();
        var rsiList     = candles.GetRsi(RsiPeriod).ToList();

        // 3) Percorre todos os candles
        for (int i = 0; i < candles.Count; i++)
        {
            var candle = candles[i];
            var fast   = emaFastList[i];
            var slow   = emaSlowList[i];
            var rsi    = rsiList[i];

            if (fast.Ema is null || slow.Ema is null || rsi.Rsi is null)
                continue; // indicadores ainda não “prontos” neste ponto

            decimal fastNow = (decimal)fast.Ema.Value;
            decimal slowNow = (decimal)slow.Ema.Value;
            decimal rsiNow  = (decimal)rsi.Rsi.Value;

            // pega o valor imediatamente anterior com EMA calculada
            var previousFast = emaFastList
                .Take(i)
                .Where(e => e.Ema != null)
                .LastOrDefault();

            var previousSlow = emaSlowList
                .Take(i)
                .Where(e => e.Ema != null)
                .LastOrDefault();

            var previousRsi = rsiList
                .Take(i)
                .Where(r => r.Rsi != null)
                .LastOrDefault();

            if (previousFast == null || previousSlow == null || previousRsi == null)
                continue;

            // 3.1) Cruzamento de alta da EMA curta sobre a longa
            bool bullishCross =
                previousFast.Ema < previousSlow.Ema &&   // antes fast < slow
                fastNow > slowNow;                       // agora fast > slow

            // 3.2) Preço acima das duas EMAs
            bool priceAboveBothEma =
                candle.Close > fastNow &&
                candle.Close > slowNow;

            // 3.3) RSI cruzando 50 de baixo pra cima
            bool rsiCrossUp50 =
                previousRsi.Rsi < 50f &&
                rsiNow > 50m &&
                rsiNow < 70m;

            // 3.4) Volume acima da média dos últimos 12 meses até esse ponto
            var last12Volumes = candles
                .Take(i + 1)
                .Reverse()
                .Take(12)
                .Select(c => c.Volume)
                .ToList();

            decimal avg12Volume = last12Volumes.Any() ? last12Volumes.Average() : 0m;
            bool volumeAboveAvg = avg12Volume > 0 && candle.Volume > avg12Volume;

            // 3.5) Se tudo foi atendido, registra oportunidade
            if (bullishCross && priceAboveBothEma && rsiCrossUp50 && volumeAboveAvg)
            {
                opportunities.Add(new OpportunitySignal
                {
                    Ticker    = ticker,
                    Date      = candle.Date,
                    Type      = OpportunityType.Call,
                    LastPrice = (double)candle.Close,
                    Reason =
                        "Oportunidade no mensal: EMA curta cruzando acima da EMA longa, " +
                        "preço acima das duas EMAs, RSI cruzando 50 pra cima e " +
                        "volume acima da média dos últimos 12 meses."
                });
            }
        }

        return opportunities;
    }
}
