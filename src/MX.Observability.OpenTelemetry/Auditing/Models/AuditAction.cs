namespace MX.Observability.OpenTelemetry.Auditing.Models;

/// <summary>
/// The type of action performed.
/// </summary>
public enum AuditAction
{
    Create,
    Read,
    Update,
    Delete,
    Execute,
    Connect,
    Disconnect,
    Moderate,
    Import,
    Export
}
