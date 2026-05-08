using System.Diagnostics;
using MX.Observability.OpenTelemetry.Filtering;
using MX.Observability.OpenTelemetry.Filtering.Configuration;

namespace MX.Observability.OpenTelemetry.Tests.Filtering;

[Trait("Category", "Unit")]
public class RequestFilterTests
{
    private static ParsedFilterRules CreateRules(Action<TelemetryFilterOptions>? configure = null)
    {
        var options = new TelemetryFilterOptions
        {
            Enabled = true,
            Requests = new RequestFilterOptions
            {
                Enabled = true,
                DurationThresholdMs = 1000,
                SuccessOnly = true,
                ExcludedPaths = "/healthz,/health",
                ExcludedHttpMethods = "",
                RetainedStatusCodes = "",
                RetainedStatusCodeRanges = ""
            }
        };
        configure?.Invoke(options);
        return ParsedFilterRules.From(options);
    }

    [Fact]
    public void ShouldFilter_HealthCheckPath_AlwaysFiltered()
    {
        var rules = CreateRules();
        var req = CreateRequestActivity(durationMs: 5000, path: "/healthz", method: "GET", statusCode: 500, status: ActivityStatusCode.Error);

        Assert.True(TracingFilterProcessor.ShouldFilterRequest(req, rules));
    }

    [Fact]
    public void ShouldFilter_SuccessfulFastRequest_ReturnsTrue()
    {
        var rules = CreateRules();
        var req = CreateRequestActivity(durationMs: 50, path: "/api/data", method: "GET", statusCode: 200, status: ActivityStatusCode.Unset);

        Assert.True(TracingFilterProcessor.ShouldFilterRequest(req, rules));
    }

    [Fact]
    public void ShouldFilter_FailedRequest_ReturnsFalse()
    {
        var rules = CreateRules(o => o.Requests.RetainedStatusCodeRanges = "400-599");
        var req = CreateRequestActivity(durationMs: 50, path: "/api/data", method: "GET", statusCode: 500, status: ActivityStatusCode.Error);

        Assert.False(TracingFilterProcessor.ShouldFilterRequest(req, rules));
    }

    [Fact]
    public void ShouldFilter_SlowRequest_ReturnsFalse()
    {
        var rules = CreateRules();
        var req = CreateRequestActivity(durationMs: 2000, path: "/api/data", method: "GET", statusCode: 200, status: ActivityStatusCode.Unset);

        Assert.False(TracingFilterProcessor.ShouldFilterRequest(req, rules));
    }

    [Fact]
    public void ShouldFilter_ExcludedHttpMethod_AlwaysFiltered()
    {
        var rules = CreateRules(o => o.Requests.ExcludedHttpMethods = "OPTIONS,HEAD");
        var req = CreateRequestActivity(durationMs: 5000, path: "/api/data", method: "OPTIONS", statusCode: 200, status: ActivityStatusCode.Unset);

        Assert.True(TracingFilterProcessor.ShouldFilterRequest(req, rules));
    }

    [Fact]
    public void ShouldFilter_RetainedStatusCode_ReturnsFalse()
    {
        var rules = CreateRules(o => o.Requests.RetainedStatusCodes = "401,403");
        var req = CreateRequestActivity(durationMs: 50, path: "/api/data", method: "GET", statusCode: 401, status: ActivityStatusCode.Unset);

        Assert.False(TracingFilterProcessor.ShouldFilterRequest(req, rules));
    }

    [Fact]
    public void ShouldFilter_DisabledRequestFilter_ReturnsFalse()
    {
        var rules = CreateRules(o => o.Requests.Enabled = false);
        var req = CreateRequestActivity(durationMs: 50, path: "/api/data", method: "GET", statusCode: 200, status: ActivityStatusCode.Unset);

        Assert.False(TracingFilterProcessor.ShouldFilterRequest(req, rules));
    }

    private static Activity CreateRequestActivity(double durationMs, string path, string method, int statusCode, ActivityStatusCode status)
    {
        var start = DateTime.UtcNow;
        var end = start.AddMilliseconds(durationMs);

        var activity = new Activity($"{method} {path}");
        activity.SetStartTime(start);
        activity.Start();
        activity.SetTag("http.route", path);
        activity.SetTag("http.request.method", method);
        activity.SetTag("http.response.status_code", statusCode);
        activity.SetStatus(status);
        activity.SetEndTime(end);
        activity.Stop();

        return activity;
    }
}
