using HVT.Core.Domain.Interfaces;
using HVT.Core.Domain.Models;
using HVT.Simulator.Extensions;

namespace HVT.Simulator;

public class MarketDataGenerator : IMarketDataGenerator
{
    private readonly Random _random = new();

    public MarketData GenerateNextTick(MarketData current, OrderBook orderBook)
    {
        const double dt = 1.0 / (252.0 * 24.0 * 60.0); // 1 minute in trading year fraction
        const double volatility = 0.20; // 20% annual volatility
        const double drift = 0.0; // No drift for short-term simulation

        var randomComponent = _random.NextGaussian();
        var priceChange = (decimal)(drift * dt + volatility * Math.Sqrt(dt) * randomComponent);

        var newPrice = current.LastPrice * (1m + priceChange);

        newPrice = Math.Max(newPrice, current.LastPrice * 0.95m);
        newPrice = Math.Min(newPrice, current.LastPrice * 1.05m);

        var spread = Math.Max(0.01m, newPrice * 0.001m);
        var bidPrice = newPrice - spread / 2m;
        var askPrice = newPrice + spread / 2m;

        var bidSize = 1000m + (decimal)(_random.NextDouble() * (double)4000m);
        var askSize = 1000m + (decimal)(_random.NextDouble() * (double)4000m);

        return new MarketData(
            current.Symbol,
            Math.Round(bidPrice, 2),
            Math.Round(askPrice, 2),
            Math.Round(newPrice, 2),
            bidSize,
            askSize,
            DateTime.UtcNow);
    }

    public OrderBook GenerateInitialOrderBook(Instrument instrument, decimal initialPrice)
    {
        var bids = new List<OrderBookLevel>();
        var asks = new List<OrderBookLevel>();

        for (var i = 1; i <= 1000; i++)
        {
            var bidPrice = Math.Round(initialPrice - i * instrument.TickSize, 2);
            var askPrice = Math.Round(initialPrice + i * instrument.TickSize, 2);

            var bidQuantity = GenerateRandomQuantity(1000m, 5000m);
            var askQuantity = GenerateRandomQuantity(1000m, 5000m);

            bids.Add(new OrderBookLevel(bidPrice, bidQuantity));
            asks.Add(new OrderBookLevel(askPrice, askQuantity));
        }

        bids = bids.OrderByDescending(b => b.Price).ToList();
        asks = asks.OrderBy(a => a.Price).ToList();

        return new OrderBook(instrument.Symbol, bids, asks, DateTime.UtcNow);
    }

    private decimal GenerateRandomQuantity(decimal min, decimal max)
    {
        return min + (decimal)(_random.NextDouble() * (double)(max - min));
    }
}