using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MX.Observability.OpenTelemetry.Filtering;
using MX.Observability.OpenTelemetry.Filtering.Configuration;

namespace MX.Observability.OpenTelemetry.Tests.Filtering;

[Trait("Category", "Unit")]
public class TelemetryFilterProcessorTests
{
    [Fact]
    public void SectionName_UsesOpenTelemetryFilteringPrefix()
    {
        Assert.Equal("OpenTelemetry:Filtering", TelemetryFilterOptions.SectionName);
    }

    [Fact]
    public void TracingProcessor_CanBeConstructedFromOptionsMonitor()
    {
        var options = new TelemetryFilterOptions
        {
            Enabled = true,
            Dependencies = new DependencyFilterOptions { Enabled = true }
        };
        var monitor = Mock.Of<IOptionsMonitor<TelemetryFilterOptions>>(m => m.CurrentValue == options);
        var logger = Mock.Of<ILogger<TracingFilterProcessor>>();

        var processor = new TracingFilterProcessor(monitor, logger);

        Assert.NotNull(processor);
    }

    [Fact]
    public void ShouldFilterRequest_RetainsServerErrors()
    {
        var rules = ParsedFilterRules.From(new TelemetryFilterOptions());
        var start = DateTime.UtcNow;
        var activity = new Activity("GET /api/resource");
        activity.SetStartTime(start);
        activity.Start();
        activity.SetTag("http.route", "/api/resource");
        activity.SetTag("http.request.method", "GET");
        activity.SetTag("http.response.status_code", 500);
        activity.SetStatus(ActivityStatusCode.Error);
        activity.SetEndTime(start.AddMilliseconds(10));
        activity.Stop();

        var shouldFilter = TracingFilterProcessor.ShouldFilterRequest(activity, rules);
        Assert.False(shouldFilter);
    }
}
