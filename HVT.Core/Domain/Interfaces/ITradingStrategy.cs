using HVT.Core.Domain.Configs;
using HVT.Core.Strategies;

namespace HVT.Core.Domain.Interfaces;

public interface ITradingStrategy
{
    string StrategyName { get; }
    Task<StrategyResult> ExecuteAsync(IMarketSimulator simulator, CancellationToken cancellationToken);
}