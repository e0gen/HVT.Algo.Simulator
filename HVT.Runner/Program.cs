using HVT.Core.Domain.Configs;
using HVT.Core.Domain.Interfaces;
using HVT.Core.Services;
using HVT.Core.Strategies;
using HVT.Simulator;

namespace HVT.Runner;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        using var scope = host.Services.CreateScope();
        var engine = scope.ServiceProvider.GetRequiredService<TradingEngine>();
        var reportGenerator = scope.ServiceProvider.GetRequiredService<PerformanceReportGenerator>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Starting HVT Trading Algorithm MVP");

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            var result = await engine.ExecuteAsync(cts.Token);

            var report = reportGenerator.GenerateReport(result);

            Console.WriteLine(report);
            await File.WriteAllTextAsync("performance_report.md", report, cts.Token);

            logger.LogInformation("Algorithm execution completed successfully. Report saved to performance_report.md");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error during algorithm execution");
            Environment.Exit(1);
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;
                configuration.ConfigureMarketSimulation(services);
                configuration.ConfigureMomentumIgnitionStrategy(services);

                services.AddSingleton<IMarketSimulator, MarketSimulator>();
                services.AddSingleton<ILiquidityProvider, LiquidityProvider>();
                services.AddSingleton<IMarketDataGenerator, MarketDataGenerator>();
                services.AddSingleton<IPriceImpactCalculator, PriceImpactCalculator>();

                services.AddTransient<ITradingStrategy, MomentumIgnitionStrategy>();
                services.AddTransient<TradingEngine>();
                services.AddTransient<PerformanceReportGenerator>();
            });
    }
}