namespace HVT.Core.Domain.Models;

public record OrderBook(
    string Symbol,
    List<OrderBookLevel> Bids,
    List<OrderBookLevel> Asks,
    DateTime Timestamp)
{
    public decimal BestBid => Bids.FirstOrDefault()?.Price ?? 0m;
    public decimal BestAsk => Asks.FirstOrDefault()?.Price ?? decimal.MaxValue;
    public decimal Spread => BestAsk - BestBid;
    public decimal MidPrice => (BestBid + BestAsk) / 2m;
}