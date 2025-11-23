using MarketAnalystBot.Infrastructure.Brapi.Models;

namespace MarketAnalystBot.Application.Contracts;

public interface IBrapiClient
{
    Task<BrapiQuoteResult?> GetDailyHistoryAsync(string ticker, string range = "3mo", string interval = "1d");
}
