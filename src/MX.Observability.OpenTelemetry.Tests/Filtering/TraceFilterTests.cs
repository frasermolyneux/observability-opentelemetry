using Microsoft.Extensions.Logging;
using MX.Observability.OpenTelemetry.Filtering;
using MX.Observability.OpenTelemetry.Filtering.Configuration;

namespace MX.Observability.OpenTelemetry.Tests.Filtering;

[Trait("Category", "Unit")]
public class TraceFilterTests
{
    private static ParsedFilterRules CreateRules(Action<TelemetryFilterOptions>? configure = null)
    {
        var options = new TelemetryFilterOptions
        {
            Enabled = true,
            Logs = new LogFilterOptions
            {
                Enabled = true,
                MinSeverity = "Warning",
                AlwaysRetainCategories = "",
                ExcludedCategories = "",
                ExcludedMessageContains = ""
            }
        };
        configure?.Invoke(options);
        return ParsedFilterRules.From(options);
    }

    [Fact]
    public void ShouldFilter_BelowMinSeverity_ReturnsTrue()
    {
        var rules = CreateRules();
        var shouldFilter = LogRecordFilterProcessor.ShouldFilterLog(
            LogLevel.Information,
            "Test.Category",
            "info message",
            attributes: null,
            rules);

        Assert.True(shouldFilter);
    }

    [Fact]
    public void ShouldFilter_AtMinSeverity_ReturnsFalse()
    {
        var rules = CreateRules();
        var shouldFilter = LogRecordFilterProcessor.ShouldFilterLog(
            LogLevel.Warning,
            "Test.Category",
            "warning message",
            attributes: null,
            rules);

        Assert.False(shouldFilter);
    }

    [Fact]
    public void ShouldFilter_AboveMinSeverity_ReturnsFalse()
    {
        var rules = CreateRules();
        var shouldFilter = LogRecordFilterProcessor.ShouldFilterLog(
            LogLevel.Error,
            "Test.Category",
            "error message",
            attributes: null,
            rules);

        Assert.False(shouldFilter);
    }

    [Fact]
    public void ShouldFilter_ExcludedCategory_AlwaysFiltered()
    {
        var rules = CreateRules(o => o.Logs.ExcludedCategories = "Microsoft.AspNetCore,System.Net.Http");
        var shouldFilter = LogRecordFilterProcessor.ShouldFilterLog(
            LogLevel.Critical,
            "Microsoft.AspNetCore",
            "request started",
            attributes: null,
            rules);

        Assert.True(shouldFilter);
    }

    [Fact]
    public void ShouldFilter_RetainedCategory_NeverFiltered()
    {
        var rules = CreateRules(o => o.Logs.AlwaysRetainCategories = "MyApp.Critical");
        var shouldFilter = LogRecordFilterProcessor.ShouldFilterLog(
            LogLevel.Trace,
            "MyApp.Critical",
            "verbose retained",
            attributes: null,
            rules);

        Assert.False(shouldFilter);
    }

    [Fact]
    public void ShouldFilter_ExcludedMessageSubstring_Filtered()
    {
        var rules = CreateRules(o => o.Logs.ExcludedMessageContains = "heartbeat,ping");
        var shouldFilter = LogRecordFilterProcessor.ShouldFilterLog(
            LogLevel.Critical,
            "Any.Category",
            "Sending heartbeat to server",
            attributes: null,
            rules);

        Assert.True(shouldFilter);
    }

    [Fact]
    public void ShouldFilter_AuditEvent_IsRetained()
    {
        var rules = CreateRules();
        var attributes = new List<KeyValuePair<string, object?>>
        {
            new("Audit.IsAuditEvent", true)
        };
        var shouldFilter = LogRecordFilterProcessor.ShouldFilterLog(
            LogLevel.Information,
            "Any.Category",
            "audit event",
            attributes,
            rules);

        Assert.False(shouldFilter);
    }
}
