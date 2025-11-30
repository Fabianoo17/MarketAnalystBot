using MarketAnalystBot.Application.Contracts;
using MarketAnalystBot.Domain.Entities;
using MarketAnalystBot.Infrastructure.Brapi.Models;
using Skender.Stock.Indicators;

namespace MarketAnalystBot.Application.Services;

public class OpportunityEngine : IOpportunityEngine
{
    private const int ShortEmaPeriod = 12;
    private const int LongEmaPeriod = 26;

    public OpportunitySignal Analyze(BrapiQuoteResult quote)
    {
        var candles = quote.HistoricalDataPrice.Select(x => new Quote
        {
            Date = x.DateUtc,
            Close = x.Close ?? 0,
            Low = x.Low ?? 0,
            High = x.High ?? 0,
            Open = x.Open ?? 0,
            Volume = x.Volume ?? 0,
        }).ToList();

        var closes = candles.OrderBy(x => x.Date)
            .Select(h => (double)h.Close)
            .ToList();

        if (closes.Count < LongEmaPeriod + 1)
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

        var rsi = candles.GetStochRsi(14, 14, 3, 3).ToList();
        var emaShort = candles.GetEma(ShortEmaPeriod).ToList();
        var emaLong = candles.GetEma(LongEmaPeriod).ToList();

        int last = closes.Count - 2;
        int prev = last - 1;

        double lastRsi = rsi[last].StochRsi!.Value;
        double prevRsi = rsi[prev].StochRsi!.Value;

        double lastShort = emaShort[last].Ema!.Value;
        double prevShort = emaShort[prev].Ema!.Value;
        double lastLong = emaLong[last].Ema!.Value;
        double prevLong = emaLong[prev].Ema!.Value;

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
        if(quote is null) return new List<OpportunitySignal> { };
        var candles = quote.HistoricalDataPrice
                           .OrderBy(h => h.DateUtc)
                           .Select(x => new Quote
                           {
                               Date = x.DateUtc,
                               Open = x.Open ?? 0,
                               High = x.High ?? 0,
                               Low = x.Low ?? 0,
                               Close = x.Close ?? 0,
                               Volume = x.Volume ?? 0
                           })
                           .ToList();

        var closes = candles
            .Select(h => (double)h.Close)
            .ToList();

        var signals = new List<OpportunitySignal>();

        if (candles.Count < 60)
            return signals;

        var stochRsiList = candles
            .GetStochRsi(14, 14, 3, 3)
            .ToList();

        var macdList = candles
            .GetMacd(12, 26, 9)
            .ToList();

        for (int i = 1; i < candles.Count; i++)
        {
            var stochPrev = stochRsiList[i - 1];
            var stochCurr = stochRsiList[i];

            var macdPrev = macdList[i - 1];
            var macdCurr = macdList[i];

            if (!stochPrev.StochRsi.HasValue || !stochCurr.StochRsi.HasValue ||
                !macdPrev.Macd.HasValue || !macdPrev.Signal.HasValue ||
                !macdCurr.Macd.HasValue || !macdCurr.Signal.HasValue)
            {
                continue;
            }

            double prevStoch = stochPrev.StochRsi.Value;
            double currStoch = stochCurr.StochRsi.Value;
            double currRsiSignal = stochCurr.Signal.Value;

            double prevMacdVal = macdPrev.Macd.Value;
            double prevSignalVal = macdPrev.Signal.Value;
            double currMacdVal = macdCurr.Macd.Value;
            double currSignalVal = macdCurr.Signal.Value;
            double currDiff = Math.Abs(currMacdVal - currSignalVal);
            double prevDiff = Math.Abs(prevMacdVal - prevSignalVal);

            bool stochFromOversold = currStoch >= 20 && prevStoch < 20;
            bool stochFromOverbought = prevStoch > 80 && currStoch <= 80;

            bool macdCrossUp =
                currMacdVal > currSignalVal &&
                 prevDiff < 0.07 &&
                currDiff >= 0.07;

            bool macdCrossDown =
                prevMacdVal >= prevSignalVal &&
                currMacdVal < currSignalVal &&
                currDiff >= 0.09;

            bool macdBullishSetup = currMacdVal < 0 && currMacdVal > prevMacdVal;
            bool macdBearishSetup = macdCrossDown && currMacdVal > 0;

            OpportunityType type = OpportunityType.None;
            string reason;

            if (stochFromOversold && macdBullishSetup)
            {
                type = OpportunityType.Call;
                reason =
                    "StochRSI saiu de sobrevenda (<20→>=20) " +
                    "+ MACD cruzou para cima da linha de sinal abaixo da linha zero (possível reversão altista).";
            }
            else if (stochFromOverbought && macdBearishSetup)
            {
                type = OpportunityType.Put;
                reason =
                    "StochRSI saiu de sobrecompra (>80→<=80) " +
                    "+ MACD cruzou para baixo da linha de sinal acima da linha zero (possível reversão baixista).";
            }
            else
            {
                continue;
            }

            signals.Add(new OpportunitySignal
            {
                Ticker = quote.Symbol,
                Date = candles[i].Date,
                LastPrice = closes[i],
                LastRsi = currStoch,
                LastRsiSignal = currRsiSignal,
                Type = type,
                Reason = reason,
                ExitSignal = FindExitByRsiCrossDown(quote, i)
            });
        }

        return signals;
    }

    public OpportunitySignal FindExitByRsiCrossDown(
    BrapiQuoteResult quote,
    int entryIndex,
    int rsiPeriod = 14,
    double exitLevel = 70.0)
    {
        // 1) Monta candles ordenados
        var candles = quote.HistoricalDataPrice
                           .OrderBy(h => h.DateUtc)
                           .Select(x => new Quote
                           {
                               Date = x.DateUtc,
                               Open = x.Open ?? 0,
                               High = x.High ?? 0,
                               Low = x.Low ?? 0,
                               Close = x.Close ?? 0,
                               Volume = x.Volume ?? 0
                           })
                           .ToList();

        var closes = candles
            .Select(h => (double)h.Close)
            .ToList();

        // Proteções básicas
        if (!candles.Any() || entryIndex < 0 || entryIndex >= candles.Count - 1)
        {
            return new OpportunitySignal
            {
                Ticker = quote.Symbol,
                Date = candles.LastOrDefault()?.Date ?? DateTime.MinValue,
                LastPrice = closes.LastOrDefault(),
                Type = OpportunityType.None,
                Reason = "Índice de entrada inválido ou histórico insuficiente para buscar saída."
            };
        }

        // 2) Calcula RSI
        var rsiList = candles
            .GetStochRsi(14,14,9,9)
            .ToList();

        // 3) Varre a partir da barra após a entrada procurando o cruzamento pra baixo
        // RSI cruzando de cima do nível (>= exitLevel) para baixo (< exitLevel)
        for (int i = entryIndex + 1; i < candles.Count; i++)
        {
            if (i == 0)
                continue;

            var prevRsiResult = rsiList[i - 1];
            var currRsiResult = rsiList[i];
            bool currRsiAbaixo = currRsiResult.StochRsi < 80;
            bool prevRsiAcima = prevRsiResult.StochRsi >= 80;

            if (!prevRsiResult.StochRsi.HasValue || !currRsiResult.StochRsi.HasValue)
                continue;

            bool rsiCrossDown = currRsiAbaixo && prevRsiAcima;

            if (rsiCrossDown )
            {
                return new OpportunitySignal
                {
                    Ticker = quote.Symbol,
                    Date = candles[i].Date,
                    LastPrice = closes[i],
                    LastRsi = currRsiResult.StochRsi.Value,
                    LastRsiSignal = currRsiResult.Signal.Value,
                    Type = OpportunityType.Put, // aqui você pode criar um tipo específico de saída, se quiser
                    Reason = $"RSI cruzou para baixo de {exitLevel} (de {prevRsiResult.StochRsi.Value:F2} para {currRsiResult.StochRsi.Value:F2}). Sinal de saída após a entrada no índice {entryIndex}."
                };
            }
        }

        // Se não achou nenhum cruzamento, retorna o último candle como "nenhum sinal claro"
        return new OpportunitySignal
        {
            Ticker = quote.Symbol,
            Date = candles.Last().Date,
            LastPrice = closes.Last(),
            LastRsi = rsiList.LastOrDefault()?.StochRsi ?? 0,
            LastRsiSignal = rsiList.LastOrDefault()?.Signal ?? 0,
            Type = OpportunityType.None,
            Reason = $"Nenhum sinal claro de saída (RSI cruzando para baixo de {exitLevel}) após a entrada no índice {entryIndex}. Mantida até o último candle."
        };
    }

}
