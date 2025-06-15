using HVT.Core.Domain.Models;

namespace HVT.Core.Domain.Interfaces;

public interface IMarketDataGenerator
{
    MarketData GenerateNextTick(MarketData current, OrderBook orderBook);
    OrderBook GenerateInitialOrderBook(Instrument instrument, decimal initialPrice);
}