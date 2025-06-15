using HVT.Core.Domain.Interfaces;
using HVT.Core.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HVT.Simulator;

public class PriceImpactCalculator : IPriceImpactCalculator
{
    private const decimal ImpactCoefficient = 0.0001m;
    private readonly ILogger<PriceImpactCalculator> _logger;

    public PriceImpactCalculator(ILogger<PriceImpactCalculator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public decimal CalculateImpact(Order order, OrderBook orderBook)
    {
        var availableLiquidity = GetAvailableLiquidity(order, orderBook);
        if (availableLiquidity <= 0) return 0m;

        var liquidityRatio = order.Quantity / availableLiquidity;
        var baseImpact = ImpactCoefficient * (decimal)Math.Sqrt((double)liquidityRatio);

        var spreadAdjustment = orderBook.Spread / orderBook.MidPrice;
        var totalImpact = baseImpact * (1m + spreadAdjustment * 10m);

        _logger.LogDebug("Calculated price impact: {Impact:P4} for order {OrderId}",
            totalImpact, order.Id);

        return Math.Min(totalImpact, 0.05m);
    }

    public decimal CalculateSlippage(decimal quantity, OrderSide side, OrderBook orderBook)
    {
        var levels = side == OrderSide.Buy ? orderBook.Asks : orderBook.Bids;
        var remainingQuantity = quantity;
        var totalCost = 0m;
        var startPrice = side == OrderSide.Buy ? orderBook.BestAsk : orderBook.BestBid;

        foreach (var level in levels)
        {
            if (remainingQuantity <= 0) break;

            var quantityAtLevel = Math.Min(remainingQuantity, level.Quantity);
            totalCost += quantityAtLevel * level.Price;
            remainingQuantity -= quantityAtLevel;
        }

        if (remainingQuantity > 0) return decimal.MaxValue;

        var averagePrice = totalCost / quantity;
        return Math.Abs(averagePrice - startPrice) / startPrice;
    }

    private decimal GetAvailableLiquidity(Order order, OrderBook orderBook)
    {
        var levels = order.Side == OrderSide.Buy ? orderBook.Asks : orderBook.Bids;
        return levels.Sum(l => l.Quantity);
    }
}