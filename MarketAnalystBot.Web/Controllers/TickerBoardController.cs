using MarketAnalystBot.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MarketAnalystBot.Web.Controllers
{
    public class TickerBoardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IBrapiClient _brapiClient;
        private readonly IOpportunityEngine _opportunityEngine;

        public TickerBoardController(AppDbContext context, IBrapiClient brapiClient, IOpportunityEngine opportunityEngine)
        {
            _context = context;
            _brapiClient = brapiClient;
            _opportunityEngine = opportunityEngine;
        }


        public async Task<IActionResult> Index(string ticker)
        {
           
            return View();
        }

        public async Task<IActionResult> candles(string ticker)
        {
            var daily = await _brapiClient.GetDailyHistoryAsync(ticker, "2y", "1d");
            var candles = daily.HistoricalDataPrice
                .OrderBy(x => x.DateUtc)
                .Select(x => new CandleDto
                {
                    Time = new DateTimeOffset(x.DateUtc).ToUnixTimeMilliseconds(),
                    Open = x.Open ?? 0,
                    High = x.High ?? 0,
                    Low = x.Low ?? 0,
                    Close = x.Close ?? 0
                })
                .ToList();

            return Ok(candles);
        }
    }
}
public class CandleDto
{
    public long Time { get; set; }   // Unix ms
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
}