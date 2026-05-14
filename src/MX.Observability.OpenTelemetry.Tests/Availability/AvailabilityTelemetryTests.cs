using Microsoft.Extensions.Logging;
using MX.Observability.OpenTelemetry.Availability;
using System.Globalization;

namespace MX.Observability.OpenTelemetry.Tests.Availability;

[Trait("Category", "Unit")]
public class AvailabilityTelemetryTests
{
    [Fact]
    public void Track_EmitsRequiredAvailabilityProperties()
    {
        var logger = new TestLogger<OpenTelemetryAvailabilityTelemetry>();
        var sut = new OpenTelemetryAvailabilityTelemetry(logger);
        var timestamp = new DateTimeOffset(2026, 5, 14, 10, 30, 15, TimeSpan.Zero);

        sut.Track(new AvailabilityTelemetryEntry
        {
            Name = "external-health-check",
            Success = true,
            Duration = TimeSpan.FromMilliseconds(1234),
            Timestamp = timestamp,
            RunLocation = "swedencentral",
            Message = "200 OK"
        });

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.LogLevel);

        Assert.Equal("external-health-check", entry.State["microsoft.availability.name"]);
        Assert.Equal("True", entry.State["microsoft.availability.success"]);
        Assert.Equal(TimeSpan.FromMilliseconds(1234).ToString("c", CultureInfo.InvariantCulture), entry.State["microsoft.availability.duration"]);
        Assert.Equal(timestamp.UtcDateTime.ToString("o"), entry.State["microsoft.availability.testTimestamp"]);
        Assert.Equal("swedencentral", entry.State["microsoft.availability.runLocation"]);
        Assert.Equal("200 OK", entry.State["microsoft.availability.message"]);

        var generatedId = Assert.IsType<string>(entry.State["microsoft.availability.id"]);
        Assert.False(string.IsNullOrWhiteSpace(generatedId));
    }

    [Fact]
    public void Track_IncludesCustomProperties()
    {
        var logger = new TestLogger<OpenTelemetryAvailabilityTelemetry>();
        var sut = new OpenTelemetryAvailabilityTelemetry(logger);

        sut.Track(new AvailabilityTelemetryEntry
        {
            Name = "custom-prop-test",
            Success = false,
            Duration = TimeSpan.FromSeconds(2),
            Id = "custom-id-123",
            Properties = new Dictionary<string, string>
            {
                ["sitewatch.app"] = "portal-web",
                ["sitewatch.environment"] = "prd"
            }
        });

        var entry = Assert.Single(logger.Entries);
        Assert.Equal("custom-id-123", entry.State["microsoft.availability.id"]);
        Assert.Equal("portal-web", entry.State["sitewatch.app"]);
        Assert.Equal("prd", entry.State["sitewatch.environment"]);
        Assert.Equal("False", entry.State["microsoft.availability.success"]);
    }

    [Fact]
    public void Track_WithBlankName_Throws()
    {
        var logger = new TestLogger<OpenTelemetryAvailabilityTelemetry>();
        var sut = new OpenTelemetryAvailabilityTelemetry(logger);

        Assert.Throws<ArgumentException>(() =>
            sut.Track(new AvailabilityTelemetryEntry
            {
                Name = " ",
                Success = true,
                Duration = TimeSpan.FromSeconds(1)
            }));
    }

    [Fact]
    public void Track_WithNegativeDuration_Throws()
    {
        var logger = new TestLogger<OpenTelemetryAvailabilityTelemetry>();
        var sut = new OpenTelemetryAvailabilityTelemetry(logger);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sut.Track(new AvailabilityTelemetryEntry
            {
                Name = "negative-duration-test",
                Success = true,
                Duration = TimeSpan.FromMilliseconds(-1)
            }));
    }

    [Fact]
    public void Track_WithWhitespaceCustomPropertyKey_Throws()
    {
        var logger = new TestLogger<OpenTelemetryAvailabilityTelemetry>();
        var sut = new OpenTelemetryAvailabilityTelemetry(logger);

        Assert.Throws<ArgumentException>(() =>
            sut.Track(new AvailabilityTelemetryEntry
            {
                Name = "invalid-property-key-test",
                Success = true,
                Duration = TimeSpan.FromMilliseconds(100),
                Properties = new Dictionary<string, string>
                {
                    [" "] = "invalid"
                }
            }));
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var values = state as IEnumerable<KeyValuePair<string, object?>>;
            var dictionary = values?.ToDictionary(pair => pair.Key, pair => pair.Value)
                ?? new Dictionary<string, object?>();

            Entries.Add(new LogEntry(logLevel, eventId, dictionary, formatter(state, exception)));
        }

        public sealed record LogEntry(
            LogLevel LogLevel,
            EventId EventId,
            Dictionary<string, object?> State,
            string Message);

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();
            public void Dispose()
            {
            }
        }
    }
}