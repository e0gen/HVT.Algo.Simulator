namespace HVT.Core.Domain.Models;

public record Order(
    string Id,
    string Symbol,
    OrderSide Side,
    OrderType Type,
    decimal Quantity,
    decimal? Price,
    DateTime Timestamp,
    OrderStatus Status = OrderStatus.Pending,
    decimal FilledQuantity = 0m,
    decimal AveragePrice = 0m);