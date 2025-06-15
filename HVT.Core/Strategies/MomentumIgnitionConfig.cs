namespace HVT.Core.Strategies;

public record MomentumIgnitionConfig(
    string Symbol,
    decimal MaxCapital,
    decimal TargetPriceImpact,
    decimal InitialOrderSize,
    decimal OrderSizeMultiplier,
    TimeSpan OrderInterval,
    int MaxCascadeDepth,
    decimal PriceThreshold,
    decimal VolatilityMultiplier,
    decimal AggressionFactor,
    TimeSpan ExecutionWindow
);