namespace MX.Observability.OpenTelemetry.Availability;

/// <summary>
/// Represents a single availability check result.
/// </summary>
public sealed record AvailabilityTelemetryEntry
{
    /// <summary>
    /// Availability test name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Indicates whether the test succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Test execution duration.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Test timestamp. Defaults to UTC now.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optional run location (for example, region or host name).
    /// </summary>
    public string? RunLocation { get; init; }

    /// <summary>
    /// Optional detail message.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Optional availability id. If omitted, a span id or guid is used.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Optional additional dimensions emitted with the availability entry.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Properties { get; init; }
}