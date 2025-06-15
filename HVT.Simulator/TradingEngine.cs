using HVT.Core.Domain.Configs;
using HVT.Core.Domain.Interfaces;
using HVT.Core.Strategies;
using Microsoft.Extensions.Logging;

namespace HVT.Simulator;

public class TradingEngine
{
    private readonly ILogger<TradingEngine> _logger;
    private readonly IMarketSimulator _marketSimulator;
    private readonly ITradingStrategy _strategy;

    public TradingEngine(
        ILogger<TradingEngine> logger,
        IMarketSimulator marketSimulator,
        ITradingStrategy strategy)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _marketSimulator = marketSimulator ?? throw new ArgumentNullException(nameof(marketSimulator));
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    public async Task<StrategyResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting trading algorithm execution");

        await _marketSimulator.StartAsync(cancellationToken);

        try
        {
            var result = await _strategy.ExecuteAsync(_marketSimulator, cancellationToken);

            _logger.LogInformation("Algorithm execution completed successfully");
            return result;
        }
        finally
        {
            await _marketSimulator.StopAsync();
        }
    }
}