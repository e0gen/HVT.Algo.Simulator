namespace HVT.Core.Domain.Models;

public enum OrderSide
{
    Buy,
    Sell
}

public enum OrderType
{
    Market,
    Limit
}

public enum OrderStatus
{
    Pending,
    PartiallyFilled,
    Filled,
    Cancelled
}