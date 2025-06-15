namespace HVT.Core.Domain.Models;

public record Trade(
    string Id,
    string Symbol,
    decimal Price,
    decimal Quantity,
    DateTime Timestamp,
    string BuyOrderId,
    string SellOrderId);