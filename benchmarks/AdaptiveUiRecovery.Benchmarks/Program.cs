using System.Globalization;
using System.Text;

namespace AdaptiveUiRecovery.Benchmarks;

public sealed record BenchmarkSummaryRecord(
    string Configuration,
    int TotalCases,
    double Precision,
    double Recall,
    double FalseHealingRatePercent,
    double P50HealLatencyMs,
    double P95HealLatencyMs)
{
    public string ToCsvRow() => string.Join(",",
        Configuration,
        TotalCases.ToString(CultureInfo.InvariantCulture),
        Precision.ToString("F4", CultureInfo.InvariantCulture),
        Recall.ToString("F4", CultureInfo.InvariantCulture),
        FalseHealingRatePercent.ToString("F2", CultureInfo.InvariantCulture),
        P50HealLatencyMs.ToString("F2", CultureInfo.InvariantCulture),
        P95HealLatencyMs.ToString("F2", CultureInfo.InvariantCulture));
}

public static class Program
{
    public static int Main(string[] args)
    {
        string mode = GetArgValue(args, "--mode") ?? "offline";
        string outputCsv = GetArgValue(args, "--out") ?? "results_summary.csv";

        Console.WriteLine($"AdaptiveUiRecovery Benchmark Runner (mode={mode})");
        Console.WriteLine("Week 1 scaffold initialized. Tier 1/Tier 2 benchmark datasets will execute in Week 12 & Week 21.");

        if (string.Equals(mode, "offline", StringComparison.OrdinalIgnoreCase) && args.Contains("--export-template"))
        {
            var header = "Configuration,TotalCases,Precision,Recall,FalseHealingRatePercent,P50HealLatencyMs,P95HealLatencyMs";
            var placeholder = new BenchmarkSummaryRecord("Template", 0, 0.0, 0.0, 0.0, 0.0, 0.0);
            var sb = new StringBuilder();
            sb.AppendLine(header);
            sb.AppendLine(placeholder.ToCsvRow());
            File.WriteAllText(outputCsv, sb.ToString());
            Console.WriteLine($"Wrote CSV template to {Path.GetFullPath(outputCsv)}");
        }

        return 0;
    }

    private static string? GetArgValue(string[] args, string flag)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
