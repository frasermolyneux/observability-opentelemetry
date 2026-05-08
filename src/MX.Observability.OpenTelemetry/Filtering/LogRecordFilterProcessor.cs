using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Logs;
using MX.Observability.OpenTelemetry.Filtering.Configuration;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace MX.Observability.OpenTelemetry.Filtering;

/// <summary>
/// OpenTelemetry log processor that removes low-value logs while always retaining
/// warnings/errors and explicit audit events.
/// </summary>
public sealed class LogRecordFilterProcessor : BaseProcessor<LogRecord>
{
    private volatile ParsedFilterRules _rules;
    private readonly ILogger<LogRecordFilterProcessor> _logger;

    public LogRecordFilterProcessor(IOptionsMonitor<TelemetryFilterOptions> optionsMonitor, ILogger<LogRecordFilterProcessor> logger)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _rules = ParsedFilterRules.From(optionsMonitor.CurrentValue);
        optionsMonitor.OnChange(opts =>
        {
            _logger.LogDebug("TelemetryFilterOptions changed, reloading filter rules");
            _rules = ParsedFilterRules.From(opts);
        });
    }

    public override void OnEnd(LogRecord data)
    {
        var rules = _rules;

        if (!rules.Enabled || !rules.LogsEnabled)
        {
            base.OnEnd(data);
            return;
        }

        var shouldFilter = ShouldFilterLog(
                data.LogLevel,
                data.CategoryName,
                data.FormattedMessage,
                data.Attributes,
                rules);

        if (shouldFilter)
        {
            _logger.LogDebug("Filtered log: {Category} [{Level}]: {Message}", data.CategoryName, data.LogLevel, data.FormattedMessage);
        }
        else
        {
            base.OnEnd(data);
        }
    }

    internal static bool ShouldFilterLog(
        LogLevel logLevel,
        string? category,
        string? formattedMessage,
        IReadOnlyList<KeyValuePair<string, object?>>? attributes,
        ParsedFilterRules rules)
    {
        if (IsAuditEvent(attributes))
            return false;

        if (!string.IsNullOrWhiteSpace(category) && rules.LogExcludedCategories.Contains(category))
            return true;

        if (rules.LogExcludedMessageContains.Length > 0 && !string.IsNullOrWhiteSpace(formattedMessage))
        {
            foreach (var excluded in rules.LogExcludedMessageContains)
            {
                if (formattedMessage.Contains(excluded, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(category) && rules.LogAlwaysRetainCategories.Contains(category))
            return false;

        if (logLevel < rules.LogMinSeverity)
            return true;

        // The configured minimum severity has been met (or exceeded), so retain the log.
        return false;
    }

    private static bool IsAuditEvent(IReadOnlyList<KeyValuePair<string, object?>>? attributes)
    {
        if (attributes is null)
            return false;

        foreach (var item in attributes)
        {
            if (!string.Equals(item.Key, "Audit.IsAuditEvent", StringComparison.Ordinal))
                continue;

            if (item.Value is bool b)
                return b;

            if (item.Value is string s && bool.TryParse(s, out var parsed))
                return parsed;
        }

        return false;
    }
}
