using Microsoft.Extensions.Logging;
using MX.Observability.OpenTelemetry.Auditing.Models;

namespace MX.Observability.OpenTelemetry.Auditing;

/// <summary>
/// OpenTelemetry-native implementation of <see cref="IAuditLogger"/>.
/// Emits audit events through <see cref="ILogger"/> with a structured scope so exporters
/// can query consistent <c>Audit.*</c> dimensions.
/// </summary>
public sealed class OpenTelemetryAuditLogger : IAuditLogger
{
    private readonly ILogger<OpenTelemetryAuditLogger> _logger;

    public OpenTelemetryAuditLogger(ILogger<OpenTelemetryAuditLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void LogAudit(AuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        var scope = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Audit.IsAuditEvent"] = true,
            ["Audit.EventName"] = auditEvent.EventName,
            ["Audit.Category"] = auditEvent.Category.ToString(),
            ["Audit.Action"] = auditEvent.Action.ToString(),
            ["Audit.Outcome"] = auditEvent.Outcome.ToString(),
            ["Audit.ActorType"] = auditEvent.ActorType.ToString()
        };

        SetIfNotNull(scope, "Audit.ActorId", auditEvent.ActorId);
        SetIfNotNull(scope, "Audit.ActorName", auditEvent.ActorName);
        SetIfNotNull(scope, "Audit.TargetId", auditEvent.TargetId);
        SetIfNotNull(scope, "Audit.TargetType", auditEvent.TargetType);
        SetIfNotNull(scope, "Audit.TargetName", auditEvent.TargetName);
        SetIfNotNull(scope, "Audit.SourceComponent", auditEvent.SourceComponent);
        SetIfNotNull(scope, "Audit.CorrelationId", auditEvent.CorrelationId);

        foreach (var (key, value) in auditEvent.Properties)
            scope[key] = value;

        using (_logger.BeginScope(scope))
        {
            _logger.LogInformation("Audit:{AuditEventName}", auditEvent.EventName);
        }
    }

    private static void SetIfNotNull(IDictionary<string, object?> scope, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            scope[key] = value;
    }
}
