namespace HVT.Core.Domain.Models;

public record MarketData(
    string Symbol,
    decimal BidPrice,
    decimal AskPrice,
    decimal LastPrice,
    decimal BidSize,
    decimal AskSize,
    DateTime Timestamp);