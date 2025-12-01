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

        public HomeController(ILogger<HomeController> logger, IOpportunityEngine opportunetClient, IBrapiClient brapiClient)
        {
            _logger = logger;
            _opportunetClient = opportunetClient;
            _brapiClient = brapiClient;
        }

        public async Task<IActionResult> Index(string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker))
                return View(new List<OpportunitySignal>());
            var quotes = await _brapiClient.GetDailyHistoryAsync(ticker, "5y","1wk");
            var signals = _opportunetClient.AnalyzeHistory(quotes);
            return View(signals);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
