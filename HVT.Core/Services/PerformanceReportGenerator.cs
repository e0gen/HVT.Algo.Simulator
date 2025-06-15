using System.Text;
using HVT.Core.Domain.Configs;
using HVT.Core.Strategies;

namespace HVT.Core.Services;

public class PerformanceReportGenerator
{
    public string GenerateReport(StrategyResult result)
    {
        var report = new StringBuilder();

        report.AppendLine("# Trading Algorithm Performance Report");
        report.AppendLine();
        report.AppendLine($"**Generated**: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        report.AppendLine($"**Strategy**: {result.StrategyName}");
        report.AppendLine($"**Instrument**: {result.Symbol}");
        report.AppendLine();

        report.AppendLine("## Executive Summary");
        report.AppendLine();
        report.AppendLine(
            $"The {result.StrategyName} strategy was executed on {result.Symbol} with the objective of creating short-term price impact using minimal capital.");
        report.AppendLine();

        var successStatus = result.TargetAchieved ? "✅ **SUCCESS**" : "❌ **PARTIAL SUCCESS**";
        report.AppendLine($"**Status**: {successStatus}");
        report.AppendLine();

        report.AppendLine("## Key Performance Metrics");
        report.AppendLine();
        report.AppendLine("| Metric | Value |");
        report.AppendLine("|--------|-------|");
        report.AppendLine($"| **Price Impact Achieved** | {result.PriceImpactAchieved:P4} |");
        report.AppendLine($"| **Capital Utilized** | €{result.CapitalUsed:N0} |");
        report.AppendLine($"| **Capital Efficiency** | {result.EfficiencyRatio:F4} impact/€M |");
        report.AppendLine($"| **Execution Time** | {result.ExecutionTime.TotalSeconds:F1} seconds |");
        report.AppendLine($"| **Orders Executed** | {result.OrdersExecuted} |");
        report.AppendLine($"| **Initial Price** | €{result.InitialPrice:F4} |");
        report.AppendLine($"| **Final Price** | €{result.FinalPrice:F4} |");
        report.AppendLine($"| **Price Change** | €{Math.Abs(result.FinalPrice - result.InitialPrice):F4} |");
        report.AppendLine();

        report.AppendLine("## Strategy Analysis");
        report.AppendLine();
        report.AppendLine("### Momentum Ignition Strategy");
        report.AppendLine();
        report.AppendLine(
            "The Momentum Ignition strategy operates by placing strategically timed orders designed to trigger algorithmic responses from other market participants. This approach leverages market microstructure dynamics to amplify price movements with minimal capital deployment.");
        report.AppendLine();

        report.AppendLine("**Key Characteristics:**");
        report.AppendLine("- **Cascade Effect**: Orders are sized to trigger progressive responses");
        report.AppendLine("- **Timing Optimization**: Exploits market liquidity patterns");
        report.AppendLine("- **Risk Management**: Built-in capital and position controls");
        report.AppendLine();

        switch (result.EfficiencyRatio)
        {
            case > 0.1m:
                report.AppendLine(
                    "**Performance Assessment**: Excellent efficiency ratio demonstrates successful capital utilization.");
                break;
            case > 0.05m:
                report.AppendLine(
                    "**Performance Assessment**: Good efficiency ratio shows effective strategy execution.");
                break;
            default:
                report.AppendLine(
                    "**Performance Assessment**: Lower efficiency suggests market conditions may not have been optimal for this strategy.");
                break;
        }

        report.AppendLine();
        report.AppendLine("## Execution Timeline");
        report.AppendLine();
        foreach (var logEntry in result.ExecutionLog)
            report.AppendLine($"- {logEntry}");

        report.AppendLine();
        report.AppendLine("## Risk Analysis");
        report.AppendLine();
        var capitalUtilization = result.CapitalUsed / 30_000_000m * 100;
        report.AppendLine($"**Capital Utilization**: {capitalUtilization:F1}% of maximum available capital");
        report.AppendLine();

        switch (capitalUtilization)
        {
            case < 10:
                report.AppendLine("✅ **Low Risk**: Minimal capital exposure maintained throughout execution.");
                break;
            case < 25:
                report.AppendLine("⚠️ **Moderate Risk**: Reasonable capital utilization with controlled exposure.");
                break;
            default:
                report.AppendLine("🔴 **Higher Risk**: Significant capital deployment - monitor closely.");
                break;
        }

        report.AppendLine();
        report.AppendLine("## Conclusions and Recommendations");
        report.AppendLine();

        report.AppendLine(
            result.TargetAchieved
                ? "The strategy successfully achieved its price impact objective while maintaining efficient capital utilization. The momentum ignition approach proved effective under current market conditions."
                : "While the target impact was not fully achieved, the strategy demonstrated controlled risk management and provided valuable insights into market microstructure behavior.");

        report.AppendLine();
        report.AppendLine("---");
        report.AppendLine(
            "*This report was generated by the HVT Trading Algorithm MVP for demonstration purposes only.*");

        return report.ToString();
    }
}