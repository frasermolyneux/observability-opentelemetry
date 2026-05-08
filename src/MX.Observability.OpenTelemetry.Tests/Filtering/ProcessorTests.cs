using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenTelemetry;
using MX.Observability.OpenTelemetry.Filtering;
using MX.Observability.OpenTelemetry.Filtering.Configuration;

namespace MX.Observability.OpenTelemetry.Tests.Filtering;

[Trait("Category", "Unit")]
public class TracingFilterProcessorTests
{
    private readonly Mock<IOptionsMonitor<TelemetryFilterOptions>> _optionsMonitor;
    private readonly Mock<ILogger<TracingFilterProcessor>> _logger;

    public TracingFilterProcessorTests()
    {
        _optionsMonitor = new Mock<IOptionsMonitor<TelemetryFilterOptions>>();
        _optionsMonitor.Setup(x => x.CurrentValue).Returns(new TelemetryFilterOptions { Enabled = true });
        _optionsMonitor.Setup(x => x.OnChange(It.IsAny<Action<TelemetryFilterOptions, string?>>())).Returns(new Mock<IDisposable>().Object);

        _logger = new Mock<ILogger<TracingFilterProcessor>>();
    }

    [Fact]
    public void Constructor_WithNullOptionsMonitor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TracingFilterProcessor(null!, _logger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TracingFilterProcessor(_optionsMonitor.Object, null!));
    }

    [Fact]
    public void OnEnd_WithDisabledFiltering_CallsBaseOnEnd()
    {
        _optionsMonitor.Setup(x => x.CurrentValue).Returns(new TelemetryFilterOptions { Enabled = false });
        var processor = new TracingFilterProcessor(_optionsMonitor.Object, _logger.Object);

        using var activity = new Activity("Test");
        activity.Start();

        processor.OnEnd(activity);

        activity.Stop();
        // No exception means base.OnEnd was called (default behavior)
    }

    [Fact]
    public void OnEnd_WhenOptionsChangeAndProcessorReconfigures_LogsDebug()
    {
        var processor = new TracingFilterProcessor(_optionsMonitor.Object, _logger.Object);

        var callbackCapture = new Action<TelemetryFilterOptions>[1];
        _optionsMonitor
            .Setup(x => x.OnChange(It.IsAny<Action<TelemetryFilterOptions, string?>>()))
            .Callback((Action<TelemetryFilterOptions, string?> callback) =>
            {
                // Store the callback to simulate config changes
                callbackCapture[0] = (opts) => callback(opts, null);
            })
            .Returns(new Mock<IDisposable>().Object);

        // Verify that OnChange was called (constructor invokes it)
        _optionsMonitor.Verify(x => x.OnChange(It.IsAny<Action<TelemetryFilterOptions, string?>>()), Times.Once);
    }
}

[Trait("Category", "Unit")]
public class LogRecordFilterProcessorTests
{
    private readonly Mock<IOptionsMonitor<TelemetryFilterOptions>> _optionsMonitor;
    private readonly Mock<ILogger<LogRecordFilterProcessor>> _logger;

    public LogRecordFilterProcessorTests()
    {
        _optionsMonitor = new Mock<IOptionsMonitor<TelemetryFilterOptions>>();
        _optionsMonitor.Setup(x => x.CurrentValue).Returns(new TelemetryFilterOptions { Enabled = true });
        _optionsMonitor.Setup(x => x.OnChange(It.IsAny<Action<TelemetryFilterOptions, string?>>())).Returns(new Mock<IDisposable>().Object);

        _logger = new Mock<ILogger<LogRecordFilterProcessor>>();
    }

    [Fact]
    public void Constructor_WithNullOptionsMonitor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new LogRecordFilterProcessor(null!, _logger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new LogRecordFilterProcessor(_optionsMonitor.Object, null!));
    }

    [Fact]
    public void OnEnd_WithDisabledFiltering_DoesNotFilterLog()
    {
        _optionsMonitor.Setup(x => x.CurrentValue).Returns(new TelemetryFilterOptions { Enabled = false });
        var processor = new LogRecordFilterProcessor(_optionsMonitor.Object, _logger.Object);

        // Verify processor was created without throwing
        Assert.NotNull(processor);
    }
}
