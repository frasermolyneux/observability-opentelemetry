using Microsoft.Extensions.Options;

namespace MX.Observability.OpenTelemetry.Filtering.Configuration;

/// <summary>
/// Validates TelemetryFilterOptions at startup to catch configuration issues early.
/// </summary>
internal sealed class TelemetryFilterOptionsValidator : IValidateOptions<TelemetryFilterOptions>
{
    public ValidateOptionsResult Validate(string? name, TelemetryFilterOptions options)
    {
        if (options == null)
            return ValidateOptionsResult.Fail("TelemetryFilterOptions is null");

        var errors = new List<string>();

        // Validate Dependencies
        if (options.Dependencies != null)
        {
            if (options.Dependencies.DurationThresholdMs < 0)
                errors.Add("Dependencies.DurationThresholdMs must be non-negative");
        }

        // Validate Requests
        if (options.Requests != null)
        {
            if (options.Requests.DurationThresholdMs < 0)
                errors.Add("Requests.DurationThresholdMs must be non-negative");

            if (!string.IsNullOrWhiteSpace(options.Requests.RetainedStatusCodeRanges))
                ValidateStatusCodeRanges(options.Requests.RetainedStatusCodeRanges, "Requests.RetainedStatusCodeRanges", errors);
        }

        // Validate Logs
        if (options.Logs != null)
        {
            if (!string.IsNullOrWhiteSpace(options.Logs.MinSeverity))
            {
                var severity = options.Logs.MinSeverity.Trim().ToLowerInvariant();
                if (!IsValidSeverity(severity))
                    errors.Add($"Logs.MinSeverity '{options.Logs.MinSeverity}' is not a valid log level");
            }
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateStatusCodeRanges(string ranges, string fieldName, List<string> errors)
    {
        foreach (var part in ranges.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var dashIndex = part.IndexOf('-');
            if (dashIndex <= 0)
            {
                errors.Add($"{fieldName}: '{part}' is not a valid range format (expected '100-599')");
                continue;
            }

            if (!int.TryParse(part.AsSpan(0, dashIndex), out var min) ||
                !int.TryParse(part.AsSpan(dashIndex + 1), out var max))
            {
                errors.Add($"{fieldName}: '{part}' contains non-integer values");
                continue;
            }

            if (min < 100 || min > 599)
                errors.Add($"{fieldName}: '{part}' — min {min} is outside valid HTTP status code range (100-599)");

            if (max < 100 || max > 599)
                errors.Add($"{fieldName}: '{part}' — max {max} is outside valid HTTP status code range (100-599)");

            if (min > max)
                errors.Add($"{fieldName}: '{part}' — min ({min}) is greater than max ({max})");
        }
    }

    private static bool IsValidSeverity(string severity)
    {
        return severity switch
        {
            "trace" or "verbose" or "debug" or "information" or "warning" or "error" or "critical" => true,
            _ => false
        };
    }
}
