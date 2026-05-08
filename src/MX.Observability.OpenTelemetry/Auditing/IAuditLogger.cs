using MX.Observability.OpenTelemetry.Auditing.Models;

namespace MX.Observability.OpenTelemetry.Auditing;

/// <summary>
/// Emits structured audit events to OpenTelemetry as custom events
/// with standardised properties for consistent cross-application querying.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs an audit event. All events are emitted with an "Audit:" prefix
    /// and consistent "Audit.*" property keys.
    /// </summary>
    void LogAudit(AuditEvent auditEvent);
}
