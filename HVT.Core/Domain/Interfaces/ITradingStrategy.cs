using HVT.Core.Domain.Configs;

namespace HVT.Core.Domain.Interfaces;

public interface ITradingStrategy
{
    string StrategyName { get; }
    Task<StrategyResult> ExecuteAsync(IMarketSimulator simulator, CancellationToken cancellationToken);
}