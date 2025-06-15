using System.Collections.Concurrent;
using HVT.Core.Domain.Configs;
using HVT.Core.Domain.Interfaces;
using HVT.Core.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HVT.Simulator;

public class MarketSimulator : IMarketSimulator, IDisposable
{
    private readonly ILogger<MarketSimulator> _logger;
    private readonly MarketSimulationConfig _config;
    private readonly ILiquidityProvider _liquidityProvider;
    private readonly IMarketDataGenerator _marketDataGenerator;
    private readonly IPriceImpactCalculator _priceImpactCalculator;
    
    private readonly Timer _liquidityTimer;
    private readonly Timer _marketDataTimer;
    
    private readonly ConcurrentDictionary<string, MarketData> _marketData = new();
    private readonly ConcurrentDictionary<string, OrderBook> _orderBooks = new();
    private readonly ConcurrentDictionary<string, Order> _orders = new();
    private readonly ConcurrentQueue<Trade> _trades = new();

    private volatile bool _isRunning;

    public MarketSimulator(
        ILogger<MarketSimulator> logger,
        IPriceImpactCalculator priceImpactCalculator,
        ILiquidityProvider liquidityProvider,
        IMarketDataGenerator marketDataGenerator,
        MarketSimulationConfig config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _priceImpactCalculator =
            priceImpactCalculator ?? throw new ArgumentNullException(nameof(priceImpactCalculator));
        _liquidityProvider = liquidityProvider ?? throw new ArgumentNullException(nameof(liquidityProvider));
        _marketDataGenerator = marketDataGenerator ?? throw new ArgumentNullException(nameof(marketDataGenerator));
        _config = config ?? throw new ArgumentNullException(nameof(config));

        _marketDataTimer = new Timer(UpdateMarketData, null, Timeout.Infinite, Timeout.Infinite);
        _liquidityTimer = new Timer(UpdateLiquidity, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Dispose()
    {
        _marketDataTimer.Dispose();
        _liquidityTimer.Dispose();
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting market simulation for {Symbol}", _config.Instrument.Symbol);

        await InitializeMarketAsync();

        _isRunning = true;

        _marketDataTimer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
        _liquidityTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));

        _logger.LogInformation("Market simulation started successfully");
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping market simulation");

        _isRunning = false;

        _marketDataTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _liquidityTimer.Change(Timeout.Infinite, Timeout.Infinite);

        await Task.Delay(100);

        _logger.LogInformation("Market simulation stopped");
    }

    public async Task<Order> SubmitOrderAsync(Order order)
    {
        if (!_isRunning) throw new InvalidOperationException("Market simulation is not running");

        _logger.LogDebug("Submitting order: {OrderId} {Side} {Quantity} @ {Price}",
            order.Id, order.Side, order.Quantity, order.Price);

        var orderBook = GetOrderBook(order.Symbol);
        var processedOrder = await ProcessOrderAsync(order, orderBook);

        _orders.TryAdd(processedOrder.Id, processedOrder);

        return processedOrder;
    }

    public Task<bool> CancelOrderAsync(string orderId)
    {
        if (!_orders.TryGetValue(orderId, out var order) || order.Status != OrderStatus.Pending)
            return Task.FromResult(false);

        var cancelledOrder = order with { Status = OrderStatus.Cancelled };
        _orders.TryUpdate(orderId, cancelledOrder, order);

        _logger.LogDebug("Cancelled order: {OrderId}", orderId);

        return Task.FromResult(true);
    }

    public OrderBook GetOrderBook(string symbol)
    {
        return _orderBooks.TryGetValue(symbol, out var orderBook)
            ? orderBook
            : throw new ArgumentException($"Order book not found for {symbol}");
    }

    public MarketData GetMarketData(string symbol)
    {
        return _marketData.TryGetValue(symbol, out var data)
            ? data
            : throw new ArgumentException($"Market data not found for {symbol}");
    }

    public IEnumerable<Trade> GetTrades(string symbol, DateTime? from = null)
    {
        var trades = _trades.Where(t => t.Symbol == symbol);

        if (from.HasValue)
            trades = trades.Where(t => t.Timestamp >= from.Value);

        return trades.OrderBy(t => t.Timestamp);
    }

    private async Task InitializeMarketAsync()
    {
        var initialOrderBook = _marketDataGenerator.GenerateInitialOrderBook(_config.Instrument, _config.InitialPrice);
        _orderBooks.TryAdd(_config.Instrument.Symbol, initialOrderBook);

        var initialMarketData = new MarketData(
            _config.Instrument.Symbol,
            initialOrderBook.BestBid,
            initialOrderBook.BestAsk,
            _config.InitialPrice,
            initialOrderBook.Bids.First().Quantity,
            initialOrderBook.Asks.First().Quantity,
            DateTime.UtcNow);

        _marketData.TryAdd(_config.Instrument.Symbol, initialMarketData);

        await Task.CompletedTask;
    }

    private async Task<Order> ProcessOrderAsync(Order order, OrderBook orderBook)
    {
        if (order.Type == OrderType.Market) return await ExecuteMarketOrderAsync(order, orderBook);

        return order with { Status = OrderStatus.Pending };
    }

    private async Task<Order> ExecuteMarketOrderAsync(Order order, OrderBook orderBook)
    {
        var levels = order.Side == OrderSide.Buy ? orderBook.Asks.ToList() : orderBook.Bids.ToList();
        var remainingQuantity = order.Quantity;
        var totalCost = 0m;
        var filledQuantity = 0m;
        var updatedLevels = new List<OrderBookLevel>();
        
        var maxLevels = levels.Count;
        var availableLevels = levels.Take(maxLevels).ToList();

        foreach (var level in availableLevels)
        {
            if (remainingQuantity <= 0) break;

            var quantityAtLevel = Math.Min(remainingQuantity, level.Quantity);
            totalCost += quantityAtLevel * level.Price;
            filledQuantity += quantityAtLevel;
            remainingQuantity -= quantityAtLevel;

            var trade = new Trade(
                Guid.NewGuid().ToString(),
                order.Symbol,
                level.Price,
                quantityAtLevel,
                DateTime.UtcNow,
                order.Side == OrderSide.Buy ? order.Id : GenerateCounterpartyId(),
                order.Side == OrderSide.Sell ? order.Id : GenerateCounterpartyId());

            _trades.Enqueue(trade);
            
            var remainingAtLevel = level.Quantity - quantityAtLevel;
            if (remainingAtLevel > 0) updatedLevels.Add(level with { Quantity = remainingAtLevel });
        }

        var touchedLevelCount = availableLevels.Count;
        if (touchedLevelCount < levels.Count) updatedLevels.AddRange(levels.Skip(touchedLevelCount));

        var updatedOrderBook = order.Side == OrderSide.Buy
            ? orderBook with { Asks = updatedLevels }
            : orderBook with { Bids = updatedLevels };

        _orderBooks.TryUpdate(order.Symbol, updatedOrderBook, orderBook);

        var averagePrice = filledQuantity > 0 ? totalCost / filledQuantity : 0m;
        var status = remainingQuantity > 0 ? OrderStatus.PartiallyFilled : OrderStatus.Filled;
        
        var impact =
            _priceImpactCalculator.CalculateImpact(order with { FilledQuantity = filledQuantity }, updatedOrderBook);
        await ApplyPriceImpactAsync(order.Symbol, impact, order.Side);

        _logger.LogDebug(
            "Executed market order: {OrderId}, Filled: {FilledQuantity}/{TotalQuantity} @ avg {AveragePrice}, Levels consumed: {LevelsConsumed}",
            order.Id, filledQuantity, order.Quantity, averagePrice, availableLevels.Count - updatedLevels.Count);

        return order with
        {
            Status = status,
            FilledQuantity = filledQuantity,
            AveragePrice = averagePrice
        };
    }

    private string GenerateCounterpartyId()
    {
        return $"LP_{Guid.NewGuid().ToString()[..8]}";
    }

    private async Task ApplyPriceImpactAsync(string symbol, decimal impact, OrderSide side)
    {
        if (_marketData.TryGetValue(symbol, out var currentData))
        {
            var direction = side == OrderSide.Buy ? 1m : -1m;
            var newPrice = currentData.LastPrice * (1m + impact * direction);

            var updatedData = currentData with
            {
                LastPrice = Math.Round(newPrice, 2),
                Timestamp = DateTime.UtcNow
            };

            _marketData.TryUpdate(symbol, updatedData, currentData);
        }

        await Task.CompletedTask;
    }

    private void UpdateMarketData(object? _)
    {
        if (!_isRunning) return;

        try
        {
            foreach (var kvp in _marketData.ToList())
            {
                var symbol = kvp.Key;
                var currentData = kvp.Value;
                var orderBook = GetOrderBook(symbol);

                var newData = _marketDataGenerator.GenerateNextTick(currentData, orderBook);
                _marketData.TryUpdate(symbol, newData, currentData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating market data");
        }
    }

    private async void UpdateLiquidity(object? _)
    {
        try
        {
            if (!_isRunning) return;

            foreach (var (symbol, orderBook) in _orderBooks.ToList())
            {
                var marketData = GetMarketData(symbol);

                await _liquidityProvider.UpdateLiquidityAsync(orderBook, marketData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating liquidity");
        }
    }
}