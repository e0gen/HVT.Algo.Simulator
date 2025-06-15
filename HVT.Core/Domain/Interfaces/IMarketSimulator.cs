using HVT.Core.Domain.Models;

namespace HVT.Core.Domain.Interfaces;

public interface IMarketSimulator
{
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
    Task<Order> SubmitOrderAsync(Order order);
    Task<bool> CancelOrderAsync(string orderId);
    OrderBook GetOrderBook(string symbol);
    MarketData GetMarketData(string symbol);
    IEnumerable<Trade> GetTrades(string symbol, DateTime? from = null);
}