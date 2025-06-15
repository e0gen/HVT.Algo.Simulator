namespace HVT.Core.Strategies;

public record StrategyResult(
    string StrategyName,
    string Symbol,
    decimal PriceImpactAchieved,
    decimal CapitalUsed,
    TimeSpan ExecutionTime,
    decimal InitialPrice,
    decimal FinalPrice,
    int OrdersExecuted,
    decimal EfficiencyRatio,
    bool TargetAchieved,
    List<string> ExecutionLog);