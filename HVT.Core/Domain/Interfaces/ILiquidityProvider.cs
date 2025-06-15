using HVT.Core.Domain.Models;

namespace HVT.Core.Domain.Interfaces;

public interface ILiquidityProvider
{
    Task UpdateLiquidityAsync(OrderBook orderBook, MarketData marketData);
    decimal GetAvailableLiquidity(decimal price, OrderSide side, OrderBook orderBook);
}