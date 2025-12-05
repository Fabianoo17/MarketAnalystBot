using MarketAnalystBot.Web.Models.Ticker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketAnalystBot.Web.Controllers
{
    public class TickerController : Controller
    {
        private readonly AppDbContext _context;

        public TickerController(AppDbContext context)
        {
            _context = context;
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
                query = query.Where(t => t.CodTicker.Contains(codTicker));
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
                AvailableSectors = sectors,
                Resultados = resultados
            };

            return View(vm);
        }
    }
}
