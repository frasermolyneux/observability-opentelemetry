using Microsoft.Extensions.Options;
using MX.Observability.OpenTelemetry.Filtering.Configuration;

namespace MX.Observability.OpenTelemetry.Tests.Filtering.Configuration;

[Trait("Category", "Unit")]
public class TelemetryFilterOptionsValidatorTests
{
    private readonly TelemetryFilterOptionsValidator _validator = new();

    [Fact]
    public void Validate_WithValidOptions_ReturnsSuccess()
    {
        var options = new TelemetryFilterOptions
        {
            Enabled = true,
            Dependencies = new DependencyFilterOptions
            {
                DurationThresholdMs = 100
            },
            Requests = new RequestFilterOptions
            {
                DurationThresholdMs = 500,
                RetainedStatusCodeRanges = "400-499"
            },
            Logs = new LogFilterOptions
            {
                MinSeverity = "Warning"
            }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WithNegativeDurationDependencies_ReturnsFailed()
    {
        var options = new TelemetryFilterOptions
        {
            Dependencies = new DependencyFilterOptions { DurationThresholdMs = -1 }
        };

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Validate_WithNegativeDurationRequests_ReturnsFailed()
    {
        var options = new TelemetryFilterOptions
        {
            Requests = new RequestFilterOptions { DurationThresholdMs = -100 }
        };

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Validate_WithInvalidSeverity_ReturnsFailed()
    {
        var options = new TelemetryFilterOptions
        {
            Logs = new LogFilterOptions { MinSeverity = "InvalidLevel" }
        };

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Validate_WithStatusCodeRangeOutOfBounds_ReturnsFailed()
    {
        var options = new TelemetryFilterOptions
        {
            Requests = new RequestFilterOptions
            {
                RetainedStatusCodeRanges = "0-999"  // Invalid: outside HTTP range
            }
        };

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Validate_WithReversedStatusCodeRange_ReturnsFailed()
    {
        var options = new TelemetryFilterOptions
        {
            Requests = new RequestFilterOptions
            {
                RetainedStatusCodeRanges = "500-400"  // Invalid: min > max
            }
        };

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Validate_WithValidStatusCodeRange_ReturnsSuccess()
    {
        var options = new TelemetryFilterOptions
        {
            Requests = new RequestFilterOptions
            {
                RetainedStatusCodeRanges = "200-299,400-499"
            }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("trace")]
    [InlineData("verbose")]
    [InlineData("debug")]
    [InlineData("information")]
    [InlineData("warning")]
    [InlineData("error")]
    [InlineData("critical")]
    public void Validate_WithValidSeverity_ReturnsSuccess(string severity)
    {
        var options = new TelemetryFilterOptions
        {
            Logs = new LogFilterOptions { MinSeverity = severity }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }
}
