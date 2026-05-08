namespace MX.Observability.OpenTelemetry.Jobs;

/// <summary>
/// Represents an in-progress job being tracked for telemetry.
/// </summary>
public interface IJobOperation : IAsyncDisposable
{
    /// <summary>Mark the job as successfully completed. Emits Audit:JobCompleted and closes the job activity span.</summary>
    void Complete(Dictionary<string, string>? additionalMetrics = null);

    /// <summary>Mark the job as failed. Emits Audit:JobFailed and records exception details on the job activity span.</summary>
    Task FailAsync(Exception exception, Dictionary<string, string>? additionalProperties = null);
}
