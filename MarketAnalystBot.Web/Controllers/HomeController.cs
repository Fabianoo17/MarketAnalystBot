using MarketAnalystBot.Application.Contracts;
using MarketAnalystBot.Domain.Entities;
using MarketAnalystBot.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace MarketAnalystBot.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IBrapiClient _brapiClient;
        private readonly IOpportunityEngine _opportunetClient;
        private readonly AppDbContext _dbContext;

        public HomeController(ILogger<HomeController> logger,
            IOpportunityEngine opportunetClient,
            IBrapiClient brapiClient,
            AppDbContext context)
        {
            _logger = logger;
            _opportunetClient = opportunetClient;
            _brapiClient = brapiClient;
            _dbContext = context;
        }

        public async Task<IActionResult> Index(string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker))
                return View(new List<OpportunitySignal>());
            var quotes = await _brapiClient.GetDailyHistoryAsync(ticker, "5y","1wk");
            var signals = _opportunetClient.AnalyzeHistory(quotes);
            return View(signals);
        }

        public async Task<IActionResult> Privacy()
        {
            _dbContext.Tickers.RemoveRange();
            await _dbContext.SaveChangesAsync();
            var dateInsert = DateTime.Now;
            var candidates = await _brapiClient.GetTickersList();
            foreach (var candidate in candidates.Stocks.Where(x=> !x.Stock.EndsWith('F'))) {
                Console.WriteLine("Analisando --- " + candidate.Stock);
                var quote = await _brapiClient.GetDailyHistoryAsync(candidate.Stock,"2y","1d");
                if (WatchlistFilter.TryEvaluate(quote, out var list)) {
                    var ticker = new Tickers { 
                        CodTicker = candidate.Stock,
                        DataRegistro = dateInsert,
                        Score = list,
                        Sector = candidate.Sector,
                        Nome = candidate.Name,
                        Logo = candidate.Logo
                    };
                    _dbContext.Tickers.Add(ticker);
                    await _dbContext.SaveChangesAsync();

                }
                    Console.WriteLine("Analisando --- " + candidate.Stock+"---"+"Score: "+list.ToString("N2"));
            }
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
