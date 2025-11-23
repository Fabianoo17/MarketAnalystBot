using MarketAnalystBot.Infrastructure.Brapi.Models;
using MarketAnalystBot.Domain.Entities;

namespace MarketAnalystBot.Application.Contracts;

public interface IOpportunityEngine
{
    OpportunitySignal Analyze(BrapiQuoteResult quote);
    List<OpportunitySignal> AnalyzeHistory(BrapiQuoteResult quote);
}
