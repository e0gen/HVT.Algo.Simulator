using HVT.Core.Domain.Models;

namespace HVT.Core.Domain.Configs;

public record MarketSimulationConfig(
    Instrument Instrument,
    decimal InitialPrice,
    decimal VolatilityPercent,
    int OrderBookDepth,
    decimal MaxSpreadPercent,
    decimal LiquidityRefreshRate);