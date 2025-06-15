using HVT.Core.Domain.Interfaces;
using HVT.Core.Domain.Models;

namespace HVT.Simulator;

public class LiquidityProvider : ILiquidityProvider
{
    private readonly Random _random = new();
    private readonly decimal _refreshRate;

    public LiquidityProvider(decimal refreshRate = 0.1m)
    {
        _refreshRate = refreshRate;
    }

    public async Task UpdateLiquidityAsync(OrderBook orderBook, MarketData marketData)
    {
        await Task.Run(() =>
        {
            UpdateOrderBookLevels(orderBook.Bids, marketData.LastPrice, OrderSide.Buy);
            UpdateOrderBookLevels(orderBook.Asks, marketData.LastPrice, OrderSide.Sell);
        });
    }

    public decimal GetAvailableLiquidity(decimal price, OrderSide side, OrderBook orderBook)
    {
        var levels = side == OrderSide.Buy ? orderBook.Asks : orderBook.Bids;
        return levels.Where(l => side == OrderSide.Buy ? l.Price <= price : l.Price >= price)
            .Sum(l => l.Quantity);
    }

    private void UpdateOrderBookLevels(List<OrderBookLevel> levels, decimal referencePrice, OrderSide side)
    {
        for (var i = 0; i < levels.Count; i++)
        {
            if (!(_random.NextDouble() < (double)_refreshRate))
                continue;

            var currentLevel = levels[i];

            var quantityVariation = 0.8m + (decimal)(_random.NextDouble() * 0.4);
            var newQuantity = Math.Max(100m, currentLevel.Quantity * quantityVariation);

            var priceVariation = GetPriceVariation(referencePrice, side);
            var newPrice = Math.Max(0.01m, currentLevel.Price + priceVariation);

            levels[i] = new OrderBookLevel(newPrice, newQuantity);
        }
    }

    private decimal GetPriceVariation(decimal referencePrice, OrderSide side)
    {
        const decimal maxVariationPercent = 0.001m;
        var variation = (decimal)(_random.NextDouble() * 2.0 - 1.0) * maxVariationPercent * referencePrice;

        return side == OrderSide.Buy
            ? Math.Min(variation, 0)
            : Math.Max(variation, 0);
    }
}