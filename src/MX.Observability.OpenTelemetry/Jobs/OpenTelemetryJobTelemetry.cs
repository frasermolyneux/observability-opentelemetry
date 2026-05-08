using System.Diagnostics;
using MX.Observability.OpenTelemetry.Auditing;
using MX.Observability.OpenTelemetry.Auditing.Models;

namespace MX.Observability.OpenTelemetry.Jobs;

/// <summary>
/// OpenTelemetry implementation of <see cref="IJobTelemetry"/>.
/// Integrates with <see cref="IAuditLogger"/> and emits job spans via <see cref="ActivitySource"/>.
/// </summary>
public sealed class OpenTelemetryJobTelemetry : IJobTelemetry
{
    private readonly IAuditLogger _auditLogger;
    public const string ActivitySourceName = "MX.Observability.OpenTelemetry.JobTelemetry";
    private static readonly ActivitySource JobActivitySource = new(ActivitySourceName);

    public OpenTelemetryJobTelemetry(IAuditLogger auditLogger)
    {
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
    }

    public IJobOperation StartJob(string jobName, Dictionary<string, string>? properties = null)
    {
        var operation = new JobOperation(_auditLogger, jobName, properties);
        operation.TrackStart();
        return operation;
    }

    public async Task<T> ExecuteAsync<T>(string jobName, Func<Task<T>> action, Dictionary<string, string>? properties = null)
    {
        await using (var operation = StartJob(jobName, properties))
        {
            try
            {
                var result = await action().ConfigureAwait(false);
                operation.Complete();
                return result;
            }
            catch (Exception ex)
            {
                await operation.FailAsync(ex).ConfigureAwait(false);
                throw;
            }
        }
    }

    public async Task ExecuteAsync(string jobName, Func<Task> action, Dictionary<string, string>? properties = null)
    {
        await using (var operation = StartJob(jobName, properties))
        {
            try
            {
                await action().ConfigureAwait(false);
                operation.Complete();
            }
            catch (Exception ex)
            {
                await operation.FailAsync(ex).ConfigureAwait(false);
                throw;
            }
        }
    }

    private sealed class JobOperation : IJobOperation
    {
        private readonly IAuditLogger _auditLogger;
        private readonly string _jobName;
        private readonly Dictionary<string, string> _properties;
        private readonly Stopwatch _stopwatch = new();
        private readonly Activity? _activity;
        private volatile bool _completed;

        public JobOperation(IAuditLogger auditLogger, string jobName, Dictionary<string, string>? properties)
        {
            _auditLogger = auditLogger;
            _jobName = jobName;
            _properties = properties is not null ? new Dictionary<string, string>(properties) : new();
            _activity = JobActivitySource.StartActivity(jobName, ActivityKind.Internal);
        }

        public void TrackStart()
        {
            _stopwatch.Start();
            _auditLogger.LogAudit(AuditEvent.SystemAction("JobStarted", AuditAction.Execute)
                .WithService(_jobName)
                .WithSource(_jobName)
                .WithProperties(_properties)
                .Build());
        }

        public void Complete(Dictionary<string, string>? additionalMetrics = null)
        {
            lock (this)
            {
                if (_completed) return;
                _completed = true;
                _stopwatch.Stop();

                var builder = AuditEvent.SystemAction("JobCompleted", AuditAction.Execute)
                    .WithService(_jobName)
                    .WithSource(_jobName)
                    .WithProperty("DurationMs", _stopwatch.ElapsedMilliseconds.ToString())
                    .WithProperties(_properties);

                if (additionalMetrics is not null)
                    builder.WithProperties(additionalMetrics);

                _auditLogger.LogAudit(builder.Build());

                _activity?.SetTag("job.name", _jobName);
                _activity?.SetTag("job.duration_ms", _stopwatch.ElapsedMilliseconds);
                _activity?.SetStatus(ActivityStatusCode.Ok);
                _activity?.Dispose();
            }
        }

        public Task FailAsync(Exception exception, Dictionary<string, string>? additionalProperties = null)
        {
            lock (this)
            {
                if (_completed)
                    return Task.CompletedTask;
                _completed = true;
                _stopwatch.Stop();

                var builder = AuditEvent.SystemAction("JobFailed", AuditAction.Execute)
                    .WithService(_jobName)
                    .WithSource(_jobName)
                    .WithOutcome(AuditOutcome.Error)
                    .WithProperty("DurationMs", _stopwatch.ElapsedMilliseconds.ToString())
                    .WithProperty("ExceptionType", exception.GetType().Name)
                    .WithProperty("ExceptionMessage", exception.Message)
                    .WithProperties(_properties);

                if (additionalProperties is not null)
                    builder.WithProperties(additionalProperties);

                _auditLogger.LogAudit(builder.Build());

                _activity?.SetTag("job.name", _jobName);
                _activity?.SetTag("job.duration_ms", _stopwatch.ElapsedMilliseconds);
                _activity?.SetTag("job.exception_type", exception.GetType().Name);
                _activity?.SetTag("job.exception_message", exception.Message);
                _activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
                _activity?.Dispose();
            }

            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            if (!_completed)
                Complete();
            return ValueTask.CompletedTask;
        }
    }
}
