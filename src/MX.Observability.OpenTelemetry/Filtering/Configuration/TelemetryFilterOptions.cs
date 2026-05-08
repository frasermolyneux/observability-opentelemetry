namespace MX.Observability.OpenTelemetry.Filtering.Configuration;

public class TelemetryFilterOptions
{
    public const string SectionName = "OpenTelemetry:Filtering";

    public bool Enabled { get; set; } = true;
    public DependencyFilterOptions Dependencies { get; set; } = new();
    public RequestFilterOptions Requests { get; set; } = new();
    public LogFilterOptions Logs { get; set; } = new();
}
