namespace MX.Observability.OpenTelemetry.Filtering.Configuration;

public class LogFilterOptions
{
    public bool Enabled { get; set; } = true;
    public string MinSeverity { get; set; } = "Warning";
    public string AlwaysRetainCategories { get; set; } = "";
    public string ExcludedCategories { get; set; } = "";
    public string ExcludedMessageContains { get; set; } = "";
}
