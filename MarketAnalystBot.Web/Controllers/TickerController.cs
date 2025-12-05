using MarketAnalystBot.Domain.Entities;
using MarketAnalystBot.Web.Models.Ticker;
using MarketAnalystBot.Web.Models;
using Skender.Stock.Indicators;
using MarketAnalystBot.Application.Contracts;
using MarketAnalystBot.Infrastructure.Brapi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketAnalystBot.Web.Controllers
{
    public class TickerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IBrapiClient _brapiClient;
        private readonly IOpportunityEngine _opportunityEngine;

        public TickerController(AppDbContext context, IBrapiClient brapiClient, IOpportunityEngine opportunityEngine)
        {
            _context = context;
            _brapiClient = brapiClient;
            _opportunityEngine = opportunityEngine;
        }

        // /Tickers
        public async Task<IActionResult> Index(
            string? codTicker,
            string? sector,
            decimal? minScore,
            decimal? maxScore)
        {
            var query = _context.Tickers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(codTicker))
            {
                query = query.Where(t => t.CodTicker != null && t.CodTicker.Contains(codTicker));
            }

            if (!string.IsNullOrWhiteSpace(sector))
            {
                query = query.Where(t => t.Sector == sector);
            }

            if (minScore.HasValue)
            {
                query = query.Where(t => t.Score >= minScore.Value);
            }

            if (maxScore.HasValue)
            {
                query = query.Where(t => t.Score <= maxScore.Value);
            }
                var sectors = await _context
                    .Tickers
                    .Select(t => t.Sector)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToListAsync();
            var resultados = await query
                    .OrderByDescending(t => t.Score)
                    .ToListAsync();

            var vm = new TickerFilterViewModel
            {
                CodTicker = codTicker,
                Sector = sector,
                MinScore = minScore,
                MaxScore = maxScore,
                AvailableSectors = sectors.Where(s => s != null).Select(s => s!).ToList(),
                Resultados = resultados
            };

            return View(vm);
        }

        public async Task<IActionResult> Oportunities()
        {
            var analyses = await _context.OpportunityAnalyses
                .OrderByDescending(a => a.Score)
                .ToListAsync();
            return View(analyses);
        }

        public async Task<IActionResult> ProcessAnalyses()
        {
            await ProcessAnalysesAsync();
            return RedirectToAction(nameof(Oportunities));
        }

        private async Task ProcessAnalysesAsync()
        {
            var tickers = await _context.Tickers.OrderBy(t => t.CodTicker).ToListAsync();

            foreach (var t in tickers)
            {
                try
                {
                    var cod = t.CodTicker?.Trim();
                    if (string.IsNullOrWhiteSpace(cod)) continue;

                    // Get daily and weekly quotes
                    var daily = await _brapiClient.GetDailyHistoryAsync(cod, "2y", "1d");
                    var weekly = await _brapiClient.GetDailyHistoryAsync(cod, "5y", "1wk");

                    // Analyze both periods
                    var dailySignals = daily is null ? new List<OpportunitySignal>() : _opportunityEngine.AnalyzeHistory(daily);
                    var weeklySignals = weekly is null ? new List<OpportunitySignal>() : _opportunityEngine.AnalyzeHistory(weekly);

                    // Score computation (simple weighted scheme)
                    decimal score = 0m;
                    var periods = new List<string>();
                    OpportunitySignal? chosen = null;

                    if (dailySignals.Any())
                    {
                        // pick the most recent signal
                        var ds = dailySignals.OrderByDescending(s => s.Date).First();
                        chosen = ds;
                        score += 50m; // base weight for daily signal
                        periods.Add("Daily");
                    }

                    if (weeklySignals.Any())
                    {
                        var ws = weeklySignals.OrderByDescending(s => s.Date).First();
                        // if weekly confirms same direction as daily, boost
                        if (chosen != null && ws.Type == chosen.Type)
                        {
                            score += 40m; // strong confirmation
                            periods.Add("Weekly");
                        }
                        else if (chosen == null)
                        {
                            chosen = ws;
                            score += 30m; // weekly-only signal
                            periods.Add("Weekly");
                        }
                    }

                    // Additional heuristics: check MACD histogram 'decreasing selling volume'
                    bool decreasingSelling = false;
                    if (daily != null)
                    {
                        var candles = daily.HistoricalDataPrice
                            .OrderBy(h => h.DateUtc)
                            .Select(x => new Skender.Stock.Indicators.Quote
                            {
                                Date = x.DateUtc,
                                Open = x.Open ?? 0,
                                High = x.High ?? 0,
                                Low = x.Low ?? 0,
                                Close = x.Close ?? 0,
                                Volume = x.Volume ?? 0
                            }).ToList();

                        var macdList = candles.GetMacd(12, 26, 9).ToList();
                        if (macdList.Count >= 4)
                        {
                            // take last 3 histogram values (macd - signal) and check negative values becoming less negative
                            var last = macdList.Skip(Math.Max(0, macdList.Count - 3)).Select(m => (double?)(m.Macd - m.Signal)).ToList();
                            if (last.Count == 3 && last.All(v => v.HasValue))
                            {
                                var a = last[0].GetValueOrDefault();
                                var b = last[1].GetValueOrDefault();
                                var c = last[2].GetValueOrDefault();
                                if (a < 0 && b < 0 && c < 0 && Math.Abs(a) > Math.Abs(b) && Math.Abs(b) > Math.Abs(c))
                                {
                                    decreasingSelling = true;
                                }
                            }
                        }
                    }

                    if (decreasingSelling) score += 10m;

                    // Normalize/clamp score to 0-100
                    if (score > 100m) score = 100m;

                    var analysis = new OpportunityAnalysis
                    {
                        CodTicker = cod!,
                        Date = DateTime.UtcNow,
                        Score = decimal.Round(score, 2),
                        Type = chosen?.Type.ToString() ?? "None",
                        Reason = chosen?.Reason ?? (decreasingSelling ? "Diminuindo pressão vendedora no histograma MACD" : "Sem sinal"),
                        LastPrice = (decimal?)(chosen?.LastPrice ?? 0),
                        LastRsi = (decimal?)(chosen?.LastRsi ?? 0),
                        PeriodsConfirmed = string.Join(";", periods),
                        CreatedAt = DateTime.UtcNow
                    };

                    // Persist: remove previous analyses for this ticker and insert new
                    var prev = _context.OpportunityAnalyses.Where(x => x.CodTicker == cod);
                    _context.OpportunityAnalyses.RemoveRange(prev);
                    _context.OpportunityAnalyses.Add(analysis);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // log and continue - keep controller simple for now
                    Console.WriteLine($"Erro ao analisar {t.CodTicker}: {ex.Message}");
                }
            }
        }
    }
}
