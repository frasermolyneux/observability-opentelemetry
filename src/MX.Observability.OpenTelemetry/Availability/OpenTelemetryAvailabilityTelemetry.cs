using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace MX.Observability.OpenTelemetry.Availability;

/// <summary>
/// OpenTelemetry-native availability emitter.
/// Uses microsoft.availability.* dimensions recognised by Azure Monitor's ingestion pipeline.
/// </summary>
public sealed class OpenTelemetryAvailabilityTelemetry : IAvailabilityTelemetry
{
    private readonly ILogger<OpenTelemetryAvailabilityTelemetry> _logger;

    public OpenTelemetryAvailabilityTelemetry(ILogger<OpenTelemetryAvailabilityTelemetry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Track(AvailabilityTelemetryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (string.IsNullOrWhiteSpace(entry.Name))
            throw new ArgumentException("Availability name must be provided.", nameof(entry));

        if (entry.Duration < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(entry), "Availability duration cannot be negative.");

        var availabilityId = !string.IsNullOrWhiteSpace(entry.Id)
            ? entry.Id!
            : GetFallbackAvailabilityId();

        var state = new List<KeyValuePair<string, object?>>()
        {
            new("microsoft.availability.id", availabilityId),
            new("microsoft.availability.name", entry.Name),
            new("microsoft.availability.testTimestamp", entry.Timestamp.UtcDateTime.ToString("o", CultureInfo.InvariantCulture)),
            new("microsoft.availability.duration", entry.Duration.ToString("c", CultureInfo.InvariantCulture)),
            new("microsoft.availability.success", entry.Success ? "True" : "False")
        };

        if (!string.IsNullOrWhiteSpace(entry.RunLocation))
            state.Add(new KeyValuePair<string, object?>("microsoft.availability.runLocation", entry.RunLocation));

        if (!string.IsNullOrWhiteSpace(entry.Message))
            state.Add(new KeyValuePair<string, object?>("microsoft.availability.message", entry.Message));

        if (entry.Properties is not null)
        {
            foreach (var (key, value) in entry.Properties)
            {
                if (string.IsNullOrWhiteSpace(key))
                    throw new ArgumentException("Availability property keys must be non-empty.", nameof(entry));

                state.Add(new KeyValuePair<string, object?>(key, value));
            }
        }

        _logger.Log(LogLevel.Information, new EventId(0, "Availability"), state, null, static (_, _) => "Availability entry");
    }

    private static string GetFallbackAvailabilityId()
    {
        var spanId = Activity.Current?.SpanId.ToString();
        return !string.IsNullOrWhiteSpace(spanId)
            ? spanId
            : Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
    }
}