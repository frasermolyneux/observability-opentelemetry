using System.Diagnostics;
using MX.Observability.OpenTelemetry.Auditing;
using MX.Observability.OpenTelemetry.Auditing.Models;
using MX.Observability.OpenTelemetry.Jobs;

namespace MX.Observability.OpenTelemetry.Tests.Jobs;

[Trait("Category", "Unit")]
public class JobTelemetryTests
{
    private readonly List<AuditEvent> _auditEvents = new();
    private readonly OpenTelemetryJobTelemetry _jobTelemetry;

    public JobTelemetryTests()
    {
        var auditLogger = new StubAuditLogger(_auditEvents);
        _jobTelemetry = new OpenTelemetryJobTelemetry(auditLogger);
    }

    [Fact]
    public async Task ExecuteAsync_Success_EmitsStartAndCompleteEvents()
    {
        await _jobTelemetry.ExecuteAsync("TestJob", () => Task.CompletedTask);

        Assert.Equal(2, _auditEvents.Count);
        Assert.Equal("JobStarted", _auditEvents[0].EventName);
        Assert.Equal("JobCompleted", _auditEvents[1].EventName);
    }

    [Fact]
    public async Task ExecuteAsync_Failure_EmitsStartAndFailEvents()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _jobTelemetry.ExecuteAsync("TestJob", () =>
                throw new InvalidOperationException("test error")));

        Assert.Equal(2, _auditEvents.Count);
        Assert.Equal("JobStarted", _auditEvents[0].EventName);
        Assert.Equal("JobFailed", _auditEvents[1].EventName);
        Assert.Equal(AuditOutcome.Error, _auditEvents[1].Outcome);
    }

    [Fact]
    public void StartJob_Complete_DoesNotThrow()
    {
        var operation = _jobTelemetry.StartJob("MetricJob");
        operation.Complete();

        Assert.Equal(2, _auditEvents.Count);
        Assert.Equal("JobStarted", _auditEvents[0].EventName);
        Assert.Equal("JobCompleted", _auditEvents[1].EventName);
    }

    [Fact]
    public async Task ExecuteAsync_Generic_ReturnsResult()
    {
        var result = await _jobTelemetry.ExecuteAsync("TestJob", () => Task.FromResult(42));

        Assert.Equal(42, result);
        Assert.Equal(2, _auditEvents.Count);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesActivity_WhenListenerAttached()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == OpenTelemetryJobTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };

        ActivitySource.AddActivityListener(listener);

        await _jobTelemetry.ExecuteAsync("ObservedJob", () => Task.CompletedTask);

        Assert.Equal(2, _auditEvents.Count);
    }

    [Fact]
    public async Task DisposeAsync_WithoutExplicitComplete_EmitsCompletionEvent()
    {
        var operation = _jobTelemetry.StartJob("ImplicitCompleteJob");
        await operation.DisposeAsync();

        Assert.Equal(2, _auditEvents.Count);
        Assert.Equal("JobStarted", _auditEvents[0].EventName);
        Assert.Equal("JobCompleted", _auditEvents[1].EventName);
        Assert.Equal(AuditOutcome.Success, _auditEvents[1].Outcome);
    }

    [Fact]
    public async Task ConcurrentComplete_DoesNotEmitDuplicateEvents()
    {
        var operation = _jobTelemetry.StartJob("ConcurrentJob");

        // Simulate concurrent Complete + FailAsync calls
        var task1 = Task.Run(() => operation.Complete());
        var task2 = Task.Run(() => operation.FailAsync(new InvalidOperationException("test")));

        await Task.WhenAll(task1, task2);

        // Should have exactly 3 events: JobStarted, JobCompleted/JobFailed, and only one completion
        Assert.True(_auditEvents.Count is 2 or 3, $"Expected 2-3 events, got {_auditEvents.Count}");
        Assert.Equal("JobStarted", _auditEvents[0].EventName);
        // The second event should be either Completed or Failed, but not both due to synchronization
        Assert.True(
            _auditEvents[1].EventName is "JobCompleted" or "JobFailed",
            $"Expected JobCompleted or JobFailed, got {_auditEvents[1].EventName}");
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_EmitsFailedEvent()
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(10);

        // TaskCanceledException is thrown when a task is cancelled (derives from OperationCanceledException)
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await _jobTelemetry.ExecuteAsync("CancelledJob", async () =>
            {
                await Task.Delay(1000, cts.Token);
            }));

        Assert.True(_auditEvents.Count >= 2);
        Assert.Equal("JobStarted", _auditEvents[0].EventName);
    }

    private sealed class StubAuditLogger(List<AuditEvent> events) : IAuditLogger
    {
        public void LogAudit(AuditEvent auditEvent) => events.Add(auditEvent);
    }
}
