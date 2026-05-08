namespace MX.Observability.OpenTelemetry.Auditing.Models;

/// <summary>
/// The result of the audited action.
/// </summary>
public enum AuditOutcome
{
    Success,
    Failure,
    Denied,
    Error
}
