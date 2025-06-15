using HVT.Core.Strategies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HVT.Core.Domain.Configs;

public static class ConfigurationExtensions
{
    public static void ConfigureMomentumIgnitionStrategy(this IConfiguration configuration, IServiceCollection services)
    {
        var strategyConfigSection = configuration.GetSection("MomentumIgnitionStrategy");
        var strategyConfig = strategyConfigSection.Get<MomentumIgnitionConfig>();
        if (strategyConfig == null)
            throw new InvalidOperationException("MomentumIgnitionStrategy config missing or invalid.");
        services.AddSingleton(strategyConfig);
    }

    public static void ConfigureMarketSimulation(this IConfiguration configuration, IServiceCollection services)
    {
        var simulationSection = configuration.GetSection("MarketSimulation");
        if (!simulationSection.Exists())
            throw new InvalidOperationException("MarketSimulation configuration section missing.");
        var simulationConfig = simulationSection.Get<MarketSimulationConfig>();
        if (simulationConfig == null)
            throw new InvalidOperationException("MarketSimulation configuration missing or invalid.");

        services.AddSingleton(simulationConfig);
    }
}