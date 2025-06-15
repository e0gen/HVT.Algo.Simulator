using HVT.Core.Domain.Configs;
using HVT.Core.Domain.Interfaces;
using HVT.Core.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HVT.Core.Strategies;

public class MomentumIgnitionStrategy : ITradingStrategy
{
    private readonly MomentumIgnitionConfig _config;
    private readonly ILogger<MomentumIgnitionStrategy> _logger;
    private readonly IPriceImpactCalculator _priceImpactCalculator;

    public MomentumIgnitionStrategy(
        MomentumIgnitionConfig config,
        ILogger<MomentumIgnitionStrategy> logger,
        IPriceImpactCalculator priceImpactCalculator)
    {
        _config = config;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _priceImpactCalculator =
            priceImpactCalculator ?? throw new ArgumentNullException(nameof(priceImpactCalculator));
    }

    public string StrategyName => "Momentum Ignition";

    public async Task<StrategyResult> ExecuteAsync(
        IMarketSimulator simulator,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var executionLog = new List<string>();
        var ordersExecuted = 0;
        var capitalUsed = 0m;

        _logger.LogInformation("Starting Momentum Ignition Strategy for {Symbol}", _config.Symbol);
        executionLog.Add($"Strategy started at {startTime:HH:mm:ss.fff}");

        var initialMarketData = simulator.GetMarketData(_config.Symbol);
        var initialPrice = initialMarketData.LastPrice;

        var initialOrderSize = _config.InitialOrderSize;
        var orderSizeMultiplier = _config.OrderSizeMultiplier;
        var orderInterval = _config.OrderInterval;
        var maxCascadeDepth = _config.MaxCascadeDepth;

        var orderBook = simulator.GetOrderBook(_config.Symbol);
        var direction = DetermineOptimalDirection(orderBook);

        executionLog.Add(
            $"Initial price: {initialPrice:F4}, Direction: {direction}, Order book spread: {orderBook.Spread:F4}");

        try
        {
            var currentOrderSize = initialOrderSize;
            var cascadeDepth = 0;

            while (cascadeDepth < maxCascadeDepth &&
                   capitalUsed < _config.MaxCapital &&
                   DateTime.UtcNow - startTime < _config.ExecutionWindow &&
                   !cancellationToken.IsCancellationRequested)
            {
                var currentMarketData = simulator.GetMarketData(_config.Symbol);
                var currentOrderBook = simulator.GetOrderBook(_config.Symbol);

                var targetPrice = CalculateOptimalPrice(currentOrderBook, direction, cascadeDepth);

                var order = new Order(
                    Guid.NewGuid().ToString(),
                    _config.Symbol,
                    direction,
                    OrderType.Limit,
                    currentOrderSize,
                    targetPrice,
                    DateTime.UtcNow);

                var submittedOrder = await simulator.SubmitOrderAsync(order);
                ordersExecuted++;
                capitalUsed += currentOrderSize * targetPrice;

                _logger.LogDebug("Submitted order {OrderId}: {Side} {Quantity} @ {Price}",
                    submittedOrder.Id, submittedOrder.Side, submittedOrder.Quantity, submittedOrder.Price);

                executionLog.Add(
                    $"Order {ordersExecuted}: {direction} {currentOrderSize:F0} @ {targetPrice:F4} (Capital: {capitalUsed:F0})");

                await Task.Delay(orderInterval, cancellationToken);

                var newMarketData = simulator.GetMarketData(_config.Symbol);
                var priceChange = Math.Abs(newMarketData.LastPrice - currentMarketData.LastPrice);
                var priceImpact = priceChange / initialPrice;

                if (priceImpact >= _config.TargetPriceImpact)
                {
                    executionLog.Add($"Target impact achieved: {priceImpact:P4}");
                    break;
                }

                if (IsGainingMomentum(currentMarketData, newMarketData, direction))
                {
                    currentOrderSize *= orderSizeMultiplier;
                    executionLog.Add($"Momentum detected, escalating order size to {currentOrderSize:F0}");
                }

                cascadeDepth++;
            }

            var endTime = DateTime.UtcNow;
            var finalMarketData = simulator.GetMarketData(_config.Symbol);
            var finalPrice = finalMarketData.LastPrice;
            var totalPriceImpact = Math.Abs(finalPrice - initialPrice) / initialPrice;
            var efficiencyRatio = totalPriceImpact / (capitalUsed / 1_000_000m);

            executionLog.Add($"Strategy completed at {endTime:HH:mm:ss.fff}");
            executionLog.Add($"Final price: {finalPrice:F4}, Total impact: {totalPriceImpact:P4}");

            _logger.LogInformation(
                "Strategy completed. Impact: {Impact:P4}, Capital: {Capital:C0}, Efficiency: {Efficiency:F4}",
                totalPriceImpact, capitalUsed, efficiencyRatio);

            return new StrategyResult(
                StrategyName,
                _config.Symbol,
                totalPriceImpact,
                capitalUsed,
                endTime - startTime,
                initialPrice,
                finalPrice,
                ordersExecuted,
                efficiencyRatio,
                totalPriceImpact >= _config.TargetPriceImpact,
                executionLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing momentum ignition strategy");
            executionLog.Add($"Error: {ex.Message}");
            throw;
        }
    }

    private OrderSide DetermineOptimalDirection(OrderBook orderBook)
    {
        var bidVolume = orderBook.Bids.Sum(b => b.Quantity);
        var askVolume = orderBook.Asks.Sum(a => a.Quantity);

        return askVolume < bidVolume ? OrderSide.Buy : OrderSide.Sell;
    }

    private decimal CalculateOptimalPrice(OrderBook orderBook, OrderSide direction, int cascadeDepth)
    {
        var aggression = Math.Min(cascadeDepth * _config.AggressionFactor, 0.5m);

        if (direction == OrderSide.Buy)
        {
            var bestAsk = orderBook.BestAsk;
            return bestAsk * (1 + aggression * 0.01m);
        }

        var bestBid = orderBook.BestBid;
        return bestBid * (1 - aggression * 0.01m);
    }

    private bool IsGainingMomentum(MarketData before, MarketData after, OrderSide direction)
    {
        var priceChange = after.LastPrice - before.LastPrice;

        return direction switch
        {
            OrderSide.Buy => priceChange > 0 && after.AskSize < before.AskSize,
            OrderSide.Sell => priceChange < 0 && after.BidSize < before.BidSize,
            _ => false
        };
    }
}