using Skender.Stock.Indicators;
using static System.Net.Mime.MediaTypeNames;

public class OpportunityEngine
{
    private const int RsiPeriod = 14;
    private const int ShortEmaPeriod = 12;
    private const int LongEmaPeriod = 26;

    public OpportunitySignal Analyze(BrapiQuoteResult quote)
    {
        var candles = quote.HistoricalDataPrice.Select(x => new Quote
        {
            Date = x.DateUtc,
            Close = x.Close,
            Low = x.Low,
            High = x.High,
            Open = x.Open,
            Volume = x.Volume,
        }).ToList();
        var closes = candles.OrderBy(x=>x.Date)
            .Select(h => (double)h.Close)
            .ToList();

        if (closes.Count < LongEmaPeriod + 2)
        {
            return new OpportunitySignal
            {
                Ticker = quote.Symbol,
                Date = candles.LastOrDefault()?.Date ?? DateTime.MinValue,
                Type = OpportunityType.None,
                LastPrice = closes.LastOrDefault(),
                Reason = "Histórico insuficiente para análise."
            };
        }

        //var rsi = Indicators.Rsi(closes, RsiPeriod);
        //var emaShort = Indicators.Ema(closes, ShortEmaPeriod);
        //var emaLong = Indicators.Ema(closes, LongEmaPeriod);

        var rsi = candles.GetStochRsi(14,14,3,3).ToList() ;
        var emaShort = candles.GetEma(ShortEmaPeriod).ToList();
        var emaLong = candles.GetEma(LongEmaPeriod).ToList();

        int last = closes.Count - 1;
        int prev = last - 1;

        double lastRsi = rsi[last].StochRsi.Value;
        double prevRsi = rsi[prev].StochRsi.Value;

        double lastShort = emaShort[last].Ema.Value;
        double prevShort = emaShort[prev].Ema.Value;
        double lastLong = emaLong[last].Ema.Value;
        double prevLong = emaLong[prev].Ema.Value;

        bool bullishCross = prevShort < prevLong && lastShort > lastLong;
        bool bearishCross = prevShort > prevLong && lastShort < lastLong;

        bool rsiFromOversold = prevRsi < 30 && lastRsi >= 30;
        bool rsiFromOverbought = prevRsi > 70 && lastRsi <= 70;

        var signal = new OpportunitySignal
        {
            Ticker = quote.Symbol,
            Date = candles[last].Date,
            LastPrice = closes[last],
            LastRsi = lastRsi
        };

        if (bullishCross && rsiFromOversold)
        {
            signal.Type = OpportunityType.Call;
            signal.Reason =
                "RSI saiu de sobrevenda (<30→>=30) e houve cruzamento altista da EMA curta sobre a EMA longa. " +
                "Possível oportunidade de CALL (opção de alta).";
        }
        else if (bearishCross && rsiFromOverbought)
        {
            signal.Type = OpportunityType.Put;
            signal.Reason =
                "RSI saiu de sobrecompra (>70→<=70) e houve cruzamento baixista da EMA curta abaixo da EMA longa. " +
                "Possível oportunidade de PUT (opção de baixa).";
        }
        else
        {
            signal.Type = OpportunityType.None;
            signal.Reason =
                "Nenhum padrão forte de reversão (RSI + cruzamento de EMAs) encontrado na barra mais recente.";
        }

        return signal;
    }

    public List<OpportunitySignal> AnalyzeHistory(BrapiQuoteResult quote)
{
    // 1) Monta candles no formato esperado pela Skender e ordena por data
    var candles = quote.HistoricalDataPrice
                       .OrderBy(h => h.DateUtc)
                       .Select(x => new Quote
                       {
                           Date = x.DateUtc,
                           Open = x.Open,
                           High = x.High,
                           Low = x.Low,
                           Close = x.Close,
                           Volume = x.Volume
                       })
                       .ToList();

    var closes = candles
        .Select(h => (double)h.Close)
        .ToList();

    var signals = new List<OpportunitySignal>();

    if (closes.Count < LongEmaPeriod + 2) // só pra evitar histórico muito pequeno
        return signals;

    // 2) Indicadores pela Skender
    var stochRsiList = candles
        .GetStochRsi(14, 14, 3, 3)
        .ToList();

    var macdList = candles
        .GetMacd(12, 26, 9)
        .ToList();

    // 3) Varre o histórico procurando sinais
    for (int i = 1; i < candles.Count; i++)
    {
        var stochPrev = stochRsiList[i - 1];
        var stochCurr = stochRsiList[i];

        var macdPrev = macdList[i - 1];
        var macdCurr = macdList[i];

        // pula enquanto os indicadores ainda não estiverem formados
        if (!stochPrev.StochRsi.HasValue || !stochCurr.StochRsi.HasValue ||
            !macdPrev.Macd.HasValue || !macdPrev.Signal.HasValue ||
            !macdCurr.Macd.HasValue || !macdCurr.Signal.HasValue)
        {
            continue;
        }

        double prevStoch = stochPrev.StochRsi.Value;
        double currStoch = stochCurr.StochRsi.Value;

        double prevMacdVal    = macdPrev.Macd.Value;
        double prevSignalVal  = macdPrev.Signal.Value;
        double currMacdVal    = macdCurr.Macd.Value;
        double currSignalVal  = macdCurr.Signal.Value;

        // OBS: aqui assumo StochRSI em escala 0–100 -> thresholds 20/80
        bool stochFromOversold   = prevStoch < 20 && currStoch >= 20;
        bool stochFromOverbought = prevStoch > 80 && currStoch <= 80;

        bool macdBullishCross  = prevMacdVal <= prevSignalVal && currMacdVal >  currSignalVal;
        bool macdBearishCross  = prevMacdVal >= prevSignalVal && currMacdVal <  currSignalVal;

        OpportunityType type = OpportunityType.None;
        string reason;

        if (stochFromOversold && macdBullishCross)
        {
            type = OpportunityType.Call;
            reason =
                "StochRSI saiu de sobrevenda (<20→>=20) + cruzamento altista do MACD (MACD acima da linha de sinal).";
        }
        else if (stochFromOverbought && macdBearishCross)
        {
            type = OpportunityType.Put;
            reason =
                "StochRSI saiu de sobrecompra (>80→<=80) + cruzamento baixista do MACD (MACD abaixo da linha de sinal).";
        }
        else
        {
            continue; // sem sinal forte, ignora esse candle
        }

        signals.Add(new OpportunitySignal
        {
            Ticker   = quote.Symbol,
            Date     = candles[i].Date,
            LastPrice = closes[i],
            LastRsi  = (double)currStoch, // aqui é o valor do StochRSI atual
            Type     = type,
            Reason   = reason
        });
    }

    return signals;
}

}

public static class Indicators
{
    /// <summary>
    /// Calcula EMA (Exponential Moving Average) padrão.
    /// </summary>
    public static List<double> Ema(IReadOnlyList<double> closes, int period)
    {
        var result = new List<double>(closes.Count);
        if (closes.Count < period)
            return result;

        double multiplier = 2.0 / (period + 1.0);

        // Média simples inicial
        double sma = closes.Take(period).Average();
        result.Add(sma);

        for (int i = period; i < closes.Count; i++)
        {
            double emaPrev = result[^1];
            double ema = ((closes[i] - emaPrev) * multiplier) + emaPrev;
            result.Add(ema);
        }

        // Para alinhamento, vamos preencher os primeiros (period-1) como NaN
        result.InsertRange(0, Enumerable.Repeat(double.NaN, period - 1));
        return result;
    }

    /// <summary>
    /// RSI de 14 períodos (clássico).
    /// </summary>
    public static List<double> Rsi(IReadOnlyList<double> closes, int period = 14)
    {
        var rsi = new List<double>(new double[closes.Count]);

        if (closes.Count <= period)
            return rsi;

        double gainSum = 0;
        double lossSum = 0;

        // Primeiro período
        for (int i = 1; i <= period; i++)
        {
            double change = (double)(closes[i] - closes[i - 1]);
            if (change > 0)
                gainSum += change;
            else
                lossSum -= change; // change negativo
        }

        double avgGain = gainSum / period;
        double avgLoss = lossSum / period;

        rsi[period] = CalcRsi(avgGain, avgLoss);

        // Demais períodos
        for (int i = period + 1; i < closes.Count; i++)
        {
            double change = (double)(closes[i] - closes[i - 1]);

            double gain = change > 0 ? change : 0;
            double loss = change < 0 ? -change : 0;

            avgGain = ((avgGain * (period - 1)) + gain) / period;
            avgLoss = ((avgLoss * (period - 1)) + loss) / period;

            rsi[i] = CalcRsi(avgGain, avgLoss);
        }

        // Antes do índice "period" deixa como 0
        for (int i = 0; i < period; i++)
        {
            rsi[i] = 0;
        }

        return rsi;
    }

    private static double CalcRsi(double avgGain, double avgLoss)
    {
        if (avgLoss == 0)
            return 100;

        double rs = avgGain / avgLoss;
        return 100 - (100 / (1 + rs));
    }
}
