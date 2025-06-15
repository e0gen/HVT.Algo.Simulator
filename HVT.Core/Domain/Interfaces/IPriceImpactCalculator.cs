using HVT.Core.Domain.Models;

namespace HVT.Core.Domain.Interfaces;

public interface IPriceImpactCalculator
{
    decimal CalculateImpact(Order order, OrderBook orderBook);
    decimal CalculateSlippage(decimal quantity, OrderSide side, OrderBook orderBook);
}