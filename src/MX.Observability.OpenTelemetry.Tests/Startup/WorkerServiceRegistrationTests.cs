using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MX.Observability.OpenTelemetry.Availability;
using MX.Observability.OpenTelemetry.Auditing;
using MX.Observability.OpenTelemetry.Filtering.Configuration;
using MX.Observability.OpenTelemetry.Jobs;
using MX.Observability.OpenTelemetry.WorkerService;

namespace MX.Observability.OpenTelemetry.Tests.Startup;

[Trait("Category", "Unit")]
public class WorkerServiceRegistrationTests
{
    [Fact]
    public void AddObservability_RegistersCoreServices()
    {
        using var _ = TemporaryEnvironmentVariable.Set(
            "APPLICATIONINSIGHTS_CONNECTION_STRING",
            "InstrumentationKey=00000000-0000-0000-0000-000000000000");

        var services = CreateServices();

        services.AddObservability();

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IAvailabilityTelemetry>());
        Assert.NotNull(provider.GetService<IAuditLogger>());
        Assert.NotNull(provider.GetService<IJobTelemetry>());
        Assert.NotNull(provider.GetService<Microsoft.Extensions.Options.IOptionsMonitor<TelemetryFilterOptions>>());
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        return services;
    }

    private sealed class TemporaryEnvironmentVariable : IDisposable
    {
        private readonly string _key;
        private readonly string? _previousValue;

        private TemporaryEnvironmentVariable(string key, string value)
        {
            _key = key;
            _previousValue = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, value);
        }

        public static TemporaryEnvironmentVariable Set(string key, string value)
            => new(key, value);

        public void Dispose()
            => Environment.SetEnvironmentVariable(_key, _previousValue);
    }
}
