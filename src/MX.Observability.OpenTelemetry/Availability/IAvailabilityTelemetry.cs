namespace MX.Observability.OpenTelemetry.Availability;

/// <summary>
/// Emits availability checks using a transport that Azure Monitor can map to availability data.
/// </summary>
public interface IAvailabilityTelemetry
{
    /// <summary>
    /// Tracks an availability check.
    /// </summary>
    void Track(AvailabilityTelemetryEntry entry);
}