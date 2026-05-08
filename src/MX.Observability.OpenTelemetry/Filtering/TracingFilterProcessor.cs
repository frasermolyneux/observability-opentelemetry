using System.Diagnostics;
using OpenTelemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MX.Observability.OpenTelemetry.Filtering.Configuration;

namespace MX.Observability.OpenTelemetry.Filtering;

/// <summary>
/// OpenTelemetry trace processor that filters out successful, fast requests/dependencies
/// to reduce telemetry volume. Failed calls, slow calls, and error status codes are retained.
/// </summary>
public sealed class TracingFilterProcessor : BaseProcessor<Activity>
{
    private volatile ParsedFilterRules _rules;
    private readonly ILogger<TracingFilterProcessor> _logger;

    public TracingFilterProcessor(IOptionsMonitor<TelemetryFilterOptions> optionsMonitor, ILogger<TracingFilterProcessor> logger)
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

    public override void OnEnd(Activity data)
    {
        var rules = _rules;

        if (!rules.Enabled)
        {
            base.OnEnd(data);
            return;
        }

        var shouldFilter = data.Kind switch
        {
            ActivityKind.Server => ShouldFilterRequest(data, rules),
            ActivityKind.Client or ActivityKind.Producer or ActivityKind.Consumer => ShouldFilterDependency(data, rules),
            _ => false
        };

        if (shouldFilter)
        {
            _logger.LogDebug("Filtered {ActivityKind}: {DisplayName}", data.Kind, data.DisplayName);
        }
        else
        {
            base.OnEnd(data);
        }
    }

    internal static bool ShouldFilterDependency(Activity dependency, ParsedFilterRules rules)
    {
        if (!rules.DependenciesEnabled)
            return false;

        var dependencyType = ResolveDependencyType(dependency);

        // Always filter ignored targets (e.g. localhost)
        var target = ResolveDependencyTarget(dependency);
        if (!string.IsNullOrWhiteSpace(target) && rules.DependencyIgnoredTargets.Contains(target))
            return true;

        // Check if this dependency type should be filtered
        if (!rules.DependencyFilterAllTypes)
        {
            if (string.IsNullOrWhiteSpace(dependencyType))
                return false;

            var typeMatches =
                rules.DependencyExcludedTypes.Contains(dependencyType) ||
                rules.DependencyExcludedTypePrefixes.Any(p =>
                    dependencyType.StartsWith(p, StringComparison.OrdinalIgnoreCase));

            if (!typeMatches)
                return false;
        }

        // Always retain result codes of interest (e.g. 429, 503)
        if (TryGetHttpStatusCode(dependency, out var dependencyStatusCode) &&
            rules.DependencyRetainedResultCodes.Contains(dependencyStatusCode.ToString()))
            return false;

        // Always retain failed calls
        if (dependency.Status == ActivityStatusCode.Error)
            return false;

        // Always retain slow calls
        if (dependency.Duration.TotalMilliseconds > rules.DependencyDurationThresholdMs)
            return false;

        return true;
    }

    internal static bool ShouldFilterRequest(Activity request, ParsedFilterRules rules)
    {
        if (!rules.RequestsEnabled)
            return false;

        // Always filter excluded paths (health checks)
        var path = ResolveRequestPath(request);
        if (!string.IsNullOrWhiteSpace(path) && rules.RequestExcludedPaths.Any(p =>
            path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Always filter excluded HTTP methods (OPTIONS, HEAD)
        var method = ResolveRequestMethod(request);
        if (!string.IsNullOrWhiteSpace(method) && rules.RequestExcludedHttpMethods.Contains(method))
            return true;

        // Always retain specific status codes
        if (TryGetHttpStatusCode(request, out var statusCode))
        {
            if (rules.RequestRetainedStatusCodes.Contains(statusCode.ToString()))
                return false;

            foreach (var (min, max) in rules.RequestRetainedStatusCodeRanges)
            {
                if (statusCode >= min && statusCode <= max)
                    return false;
            }
        }

        // If SuccessOnly, only filter successful requests
        var isSuccessful = request.Status != ActivityStatusCode.Error;
        if (rules.RequestSuccessOnly && !isSuccessful)
            return false;

        // Always retain slow requests
        if (request.Duration.TotalMilliseconds > rules.RequestDurationThresholdMs)
            return false;

        return true;
    }

    private static bool TryGetHttpStatusCode(Activity activity, out int statusCode)
    {
        var value = activity.GetTagItem("http.response.status_code")?.ToString();
        return int.TryParse(value, out statusCode);
    }

    private static string? ResolveRequestPath(Activity request)
    {
        var route = request.GetTagItem("http.route")?.ToString();
        if (!string.IsNullOrWhiteSpace(route))
            return route;

        var rawPath = request.GetTagItem("url.path")?.ToString();
        if (!string.IsNullOrWhiteSpace(rawPath))
            return rawPath;

        var name = request.DisplayName;
        var spaceIndex = name.IndexOf(' ');
        return spaceIndex > 0 ? name[(spaceIndex + 1)..] : name;
    }

    private static string? ResolveRequestMethod(Activity request)
    {
        var method = request.GetTagItem("http.request.method")?.ToString();
        if (!string.IsNullOrWhiteSpace(method))
            return method;

        var name = request.DisplayName;
        var spaceIndex = name.IndexOf(' ');
        return spaceIndex > 0 ? name[..spaceIndex] : null;
    }

    private static string? ResolveDependencyTarget(Activity dependency)
    {
        return dependency.GetTagItem("server.address")?.ToString()
            ?? dependency.GetTagItem("net.peer.name")?.ToString()
            ?? dependency.GetTagItem("http.host")?.ToString()
            ?? dependency.GetTagItem("db.name")?.ToString();
    }

    private static string? ResolveDependencyType(Activity dependency)
    {
        return dependency.GetTagItem("db.system")?.ToString()
            ?? dependency.GetTagItem("rpc.system")?.ToString()
            ?? (dependency.GetTagItem("http.request.method") is not null ? "http" : null);
    }
}
