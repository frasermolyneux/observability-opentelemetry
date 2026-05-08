using System.Diagnostics;
using MX.Observability.OpenTelemetry.Filtering;
using MX.Observability.OpenTelemetry.Filtering.Configuration;

namespace MX.Observability.OpenTelemetry.Tests.Filtering;

[Trait("Category", "Unit")]
public class DependencyFilterTests
{
    private static ParsedFilterRules CreateRules(Action<TelemetryFilterOptions>? configure = null)
    {
        var options = new TelemetryFilterOptions
        {
            Enabled = true,
            Dependencies = new DependencyFilterOptions
            {
                Enabled = true,
                FilterAllTypes = true,
                DurationThresholdMs = 1000
            }
        };
        configure?.Invoke(options);
        return ParsedFilterRules.From(options);
    }

    [Fact]
    public void ShouldFilter_SuccessfulFastDependency_FilterAllTypes_ReturnsTrue()
    {
        var rules = CreateRules();
        var dep = CreateDependencyActivity(durationMs: 50, status: ActivityStatusCode.Unset, method: "GET");

        Assert.True(TracingFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_FailedDependency_ReturnsFalse()
    {
        var rules = CreateRules();
        var dep = CreateDependencyActivity(durationMs: 50, status: ActivityStatusCode.Error, method: "GET");

        Assert.False(TracingFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_SlowDependency_ReturnsFalse()
    {
        var rules = CreateRules();
        var dep = CreateDependencyActivity(durationMs: 2000, status: ActivityStatusCode.Unset, method: "GET");

        Assert.False(TracingFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_IgnoredTarget_AlwaysFiltered()
    {
        var rules = CreateRules(o => o.Dependencies.IgnoredTargets = "localhost,127.0.0.1");
        var dep = CreateDependencyActivity(durationMs: 5000, status: ActivityStatusCode.Error, method: "GET", target: "localhost");

        Assert.True(TracingFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_RetainedResultCode_ReturnsFalse()
    {
        var rules = CreateRules(o => o.Dependencies.RetainedResultCodes = "429,503");
        var dep = CreateDependencyActivity(durationMs: 50, status: ActivityStatusCode.Unset, method: "GET", statusCode: 429);

        Assert.False(TracingFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_DisabledDependencyFilter_ReturnsFalse()
    {
        var rules = CreateRules(o => o.Dependencies.Enabled = false);
        var dep = CreateDependencyActivity(durationMs: 50, status: ActivityStatusCode.Unset, method: "GET");

        Assert.False(TracingFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_TypeNotInExcludedList_WhenNotFilterAll_ReturnsFalse()
    {
        var rules = CreateRules(o =>
        {
            o.Dependencies.FilterAllTypes = false;
            o.Dependencies.ExcludedTypes = "SQL,Azure Table";
        });
        var dep = CreateDependencyActivity(durationMs: 50, status: ActivityStatusCode.Unset, method: "GET");

        Assert.False(TracingFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_TypeInExcludedList_WhenNotFilterAll_ReturnsTrue()
    {
        var rules = CreateRules(o =>
        {
            o.Dependencies.FilterAllTypes = false;
            o.Dependencies.ExcludedTypes = "SQL,Azure Table";
        });
        var dep = CreateDependencyActivity(durationMs: 50, status: ActivityStatusCode.Unset, dbSystem: "SQL");

        Assert.True(TracingFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_TypeMatchesPrefixExclusion_ReturnsTrue()
    {
        var rules = CreateRules(o =>
        {
            o.Dependencies.FilterAllTypes = false;
            o.Dependencies.ExcludedTypePrefixes = "Azure";
        });
        var dep = CreateDependencyActivity(durationMs: 50, status: ActivityStatusCode.Unset, dbSystem: "Azure Blob");

        Assert.True(TracingFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    private static Activity CreateDependencyActivity(
        double durationMs,
        ActivityStatusCode status,
        string? method = null,
        string? target = null,
        int? statusCode = null,
        string? dbSystem = null)
    {
        var start = DateTime.UtcNow;
        var end = start.AddMilliseconds(durationMs);

        var activity = new Activity("dependency-call");
        activity.SetStartTime(start);
        activity.Start();

        if (!string.IsNullOrWhiteSpace(method))
            activity.SetTag("http.request.method", method);

        if (!string.IsNullOrWhiteSpace(target))
            activity.SetTag("server.address", target);

        if (statusCode.HasValue)
            activity.SetTag("http.response.status_code", statusCode.Value);

        if (!string.IsNullOrWhiteSpace(dbSystem))
            activity.SetTag("db.system", dbSystem);

        activity.SetStatus(status);
        activity.SetEndTime(end);
        activity.Stop();

        return activity;
    }
}
