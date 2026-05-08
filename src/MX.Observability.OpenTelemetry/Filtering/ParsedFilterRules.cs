using Microsoft.Extensions.Logging;
using MX.Observability.OpenTelemetry.Filtering.Configuration;

namespace MX.Observability.OpenTelemetry.Filtering;

/// <summary>
/// Immutable, pre-parsed snapshot of filter rules built from <see cref="TelemetryFilterOptions"/>.
/// Rebuilt only when configuration changes. Enables fast per-item filtering via HashSet lookups.
/// </summary>
internal sealed class ParsedFilterRules
{
    // Global
    public bool Enabled { get; }

    // Dependencies
    public bool DependenciesEnabled { get; }
    public double DependencyDurationThresholdMs { get; }
    public bool DependencyFilterAllTypes { get; }
    public HashSet<string> DependencyExcludedTypes { get; }
    public string[] DependencyExcludedTypePrefixes { get; }
    public HashSet<string> DependencyIgnoredTargets { get; }
    public HashSet<string> DependencyRetainedResultCodes { get; }

    // Requests
    public bool RequestsEnabled { get; }
    public double RequestDurationThresholdMs { get; }
    public bool RequestSuccessOnly { get; }
    public string[] RequestExcludedPaths { get; }
    public HashSet<string> RequestExcludedHttpMethods { get; }
    public HashSet<string> RequestRetainedStatusCodes { get; }
    public (int Min, int Max)[] RequestRetainedStatusCodeRanges { get; }

    // Logs
    public bool LogsEnabled { get; }
    public LogLevel LogMinSeverity { get; }
    public HashSet<string> LogAlwaysRetainCategories { get; }
    public HashSet<string> LogExcludedCategories { get; }
    public string[] LogExcludedMessageContains { get; }

    private ParsedFilterRules(TelemetryFilterOptions options)
    {
        Enabled = options.Enabled;

        // Dependencies
        var deps = options.Dependencies;
        DependenciesEnabled = deps.Enabled;
        DependencyDurationThresholdMs = deps.DurationThresholdMs;
        DependencyFilterAllTypes = deps.FilterAllTypes;
        DependencyExcludedTypes = ParseCsvToHashSet(deps.ExcludedTypes);
        DependencyExcludedTypePrefixes = ParseCsvToArray(deps.ExcludedTypePrefixes);
        DependencyIgnoredTargets = ParseCsvToHashSet(deps.IgnoredTargets);
        DependencyRetainedResultCodes = ParseCsvToHashSet(deps.RetainedResultCodes);

        // Requests
        var reqs = options.Requests;
        RequestsEnabled = reqs.Enabled;
        RequestDurationThresholdMs = reqs.DurationThresholdMs;
        RequestSuccessOnly = reqs.SuccessOnly;
        RequestExcludedPaths = ParseCsvToArray(reqs.ExcludedPaths);
        RequestExcludedHttpMethods = ParseCsvToHashSet(reqs.ExcludedHttpMethods);
        RequestRetainedStatusCodes = ParseCsvToHashSet(reqs.RetainedStatusCodes);
        RequestRetainedStatusCodeRanges = ParseStatusCodeRanges(reqs.RetainedStatusCodeRanges);

        // Logs
        var logs = options.Logs;
        LogsEnabled = logs.Enabled;
        LogMinSeverity = ParseSeverity(logs.MinSeverity);
        LogAlwaysRetainCategories = ParseCsvToHashSet(logs.AlwaysRetainCategories);
        LogExcludedCategories = ParseCsvToHashSet(logs.ExcludedCategories);
        LogExcludedMessageContains = ParseCsvToArray(logs.ExcludedMessageContains);
    }

    public static ParsedFilterRules From(TelemetryFilterOptions options) => new(options);

    private static HashSet<string> ParseCsvToHashSet(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return new HashSet<string>(
            csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
    }

    private static string[] ParseCsvToArray(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return Array.Empty<string>();

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static LogLevel ParseSeverity(string? severity)
    {
        if (string.IsNullOrWhiteSpace(severity))
            return LogLevel.Warning;

        return severity.Trim().ToLowerInvariant() switch
        {
            "trace" => LogLevel.Trace,
            "verbose" => LogLevel.Trace,
            "debug" => LogLevel.Debug,
            "information" => LogLevel.Information,
            "warning" => LogLevel.Warning,
            "error" => LogLevel.Error,
            "critical" => LogLevel.Critical,
            _ => LogLevel.Warning
        };
    }

    private static (int Min, int Max)[] ParseStatusCodeRanges(string? ranges)
    {
        if (string.IsNullOrWhiteSpace(ranges))
            return Array.Empty<(int, int)>();

        var result = new List<(int Min, int Max)>();
        foreach (var part in ranges.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var dashIndex = part.IndexOf('-');
            if (dashIndex > 0 &&
                int.TryParse(part.AsSpan(0, dashIndex), out var min) &&
                int.TryParse(part.AsSpan(dashIndex + 1), out var max))
            {
                // Validate HTTP status code range (100-599) and ordering
                if (min >= 100 && max <= 599 && min <= max)
                {
                    result.Add((min, max));
                }
            }
        }
        return result.ToArray();
    }
}
