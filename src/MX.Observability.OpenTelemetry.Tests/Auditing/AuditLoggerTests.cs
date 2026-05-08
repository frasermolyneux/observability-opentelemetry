using Microsoft.Extensions.Logging;
using MX.Observability.OpenTelemetry.Auditing;
using MX.Observability.OpenTelemetry.Auditing.Models;

namespace MX.Observability.OpenTelemetry.Tests.Auditing;

[Trait("Category", "Unit")]
public class AuditLoggerTests
{
    [Fact]
    public void LogAudit_EmitsInformationLog_WithAuditPrefix()
    {
        var sink = new TestLogSink();
        var logger = new TestLogger<OpenTelemetryAuditLogger>(sink);
        var auditLogger = new OpenTelemetryAuditLogger(logger);

        var auditEvent = AuditEvent.UserAction("Login", AuditAction.Execute).Build();

        auditLogger.LogAudit(auditEvent);

        var log = Assert.Single(sink.Entries);
        Assert.Equal(LogLevel.Information, log.LogLevel);
        Assert.Equal("Audit:Login", log.Message);
    }

    [Fact]
    public void LogAudit_WritesStandardAuditPropertiesToScope()
    {
        var sink = new TestLogSink();
        var logger = new TestLogger<OpenTelemetryAuditLogger>(sink);
        var auditLogger = new OpenTelemetryAuditLogger(logger);

        var auditEvent = AuditEvent.UserAction("UpdateProfile", AuditAction.Update)
            .WithActor("user-123", "Alice")
            .WithTarget("profile-1", "Profile")
            .Build();

        auditLogger.LogAudit(auditEvent);

        var entry = Assert.Single(sink.Entries);
        Assert.True(entry.Scope.TryGetValue("Audit.IsAuditEvent", out var isAudit));
        Assert.Equal(true, isAudit);
        Assert.Equal("UpdateProfile", entry.Scope["Audit.EventName"]);
        Assert.Equal("User", entry.Scope["Audit.Category"]);
        Assert.Equal("Update", entry.Scope["Audit.Action"]);
        Assert.Equal("Success", entry.Scope["Audit.Outcome"]);
        Assert.Equal("user-123", entry.Scope["Audit.ActorId"]);
        Assert.Equal("Alice", entry.Scope["Audit.ActorName"]);
        Assert.Equal("profile-1", entry.Scope["Audit.TargetId"]);
    }

    [Fact]
    public void LogAudit_IncludesCustomProperties()
    {
        var sink = new TestLogSink();
        var logger = new TestLogger<OpenTelemetryAuditLogger>(sink);
        var auditLogger = new OpenTelemetryAuditLogger(logger);

        var auditEvent = AuditEvent.SystemAction("JobCompleted", AuditAction.Execute)
            .WithProperty("DurationMs", "123")
            .Build();

        auditLogger.LogAudit(auditEvent);

        var entry = Assert.Single(sink.Entries);
        Assert.Equal("123", entry.Scope["DurationMs"]);
    }

    [Fact]
    public void LogAudit_NullEvent_ThrowsArgumentNullException()
    {
        var sink = new TestLogSink();
        var logger = new TestLogger<OpenTelemetryAuditLogger>(sink);
        var auditLogger = new OpenTelemetryAuditLogger(logger);

        Assert.Throws<ArgumentNullException>(() => auditLogger.LogAudit(null!));
    }

    private sealed class TestLogSink
    {
        public List<LogEntry> Entries { get; } = new();

        public sealed record LogEntry(LogLevel LogLevel, string Message, Dictionary<string, object?> Scope);
    }

    private sealed class TestLogger<T>(TestLogSink sink) : ILogger<T>
    {
        private readonly Stack<Dictionary<string, object?>> _scopes = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            var scopeDictionary = new Dictionary<string, object?>();

            if (state is IEnumerable<KeyValuePair<string, object?>> kvps)
            {
                foreach (var (key, value) in kvps)
                    scopeDictionary[key] = value;
            }
            else
            {
                scopeDictionary["Scope"] = state;
            }

            _scopes.Push(scopeDictionary);
            return new ScopeDisposable(_scopes);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            var scope = _scopes.Count > 0
                ? new Dictionary<string, object?>(_scopes.Peek())
                : new Dictionary<string, object?>();

            sink.Entries.Add(new TestLogSink.LogEntry(logLevel, message, scope));
        }

        private sealed class ScopeDisposable(Stack<Dictionary<string, object?>> scopes) : IDisposable
        {
            public void Dispose()
            {
                if (scopes.Count > 0)
                    scopes.Pop();
            }
        }
    }
}
