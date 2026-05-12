using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using MX.Observability.OpenTelemetry.Extensions;
using MX.Observability.OpenTelemetry.Filtering;
using MX.Observability.OpenTelemetry.Filtering.Configuration;
using MX.Observability.OpenTelemetry.Jobs;

namespace MX.Observability.OpenTelemetry.AspNetCore;

/// <summary>ASP.NET Core registration entry point for MX Observability.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers MX Observability for an ASP.NET Core host.
    /// This configures OpenTelemetry tracing and logging with Azure Monitor exporters.
    /// </summary>
    public static IServiceCollection AddObservability(this IServiceCollection services)
    {
        ValidateAzureMonitorConfiguration(services);
        var serviceName = ResolveServiceName();

        services.AddObservabilityCore();
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
                .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource(OpenTelemetryJobTelemetry.ActivitySourceName)
                    .AddProcessor(sp => new TracingFilterProcessor(
                        sp.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<TelemetryFilterOptions>>(),
                        sp.GetRequiredService<ILogger<TracingFilterProcessor>>()))
                    .AddAzureMonitorTraceExporter();
            })
                .WithLogging(logging =>
            {
                logging
                    .AddProcessor(sp => new LogRecordFilterProcessor(
                        sp.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<TelemetryFilterOptions>>(),
                        sp.GetRequiredService<ILogger<LogRecordFilterProcessor>>()))
                    .AddAzureMonitorLogExporter();
            });

        return services;
    }

    /// <summary>
    /// Registers MX Observability for an ASP.NET Core host with custom filter configuration.
    /// </summary>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        Action<TelemetryFilterOptions> configureFiltering)
    {
        ValidateAzureMonitorConfiguration(services);
        var serviceName = ResolveServiceName();

        services.AddObservabilityCore(configureFiltering);
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
                .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource(OpenTelemetryJobTelemetry.ActivitySourceName)
                    .AddProcessor(sp => new TracingFilterProcessor(
                        sp.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<TelemetryFilterOptions>>(),
                        sp.GetRequiredService<ILogger<TracingFilterProcessor>>()))
                    .AddAzureMonitorTraceExporter();
            })
                .WithLogging(logging =>
            {
                logging
                    .AddProcessor(sp => new LogRecordFilterProcessor(
                        sp.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<TelemetryFilterOptions>>(),
                        sp.GetRequiredService<ILogger<LogRecordFilterProcessor>>()))
                    .AddAzureMonitorLogExporter();
            });

        return services;
    }

    private static void ValidateAzureMonitorConfiguration(IServiceCollection services)
    {
        var connectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(connectionString))
            return;

        if (TryGetApplicationInsightsConnectionString(services, out connectionString) &&
            !string.IsNullOrWhiteSpace(connectionString))
            return;

        throw new InvalidOperationException(
            "Azure Monitor connection string not configured. Set APPLICATIONINSIGHTS_CONNECTION_STRING environment variable or configure 'ConnectionStrings:ApplicationInsights' in appsettings.json");
    }

    private static bool TryGetApplicationInsightsConnectionString(IServiceCollection services, out string? connectionString)
    {
        connectionString = null;
        var configuration = services
            .LastOrDefault(d => d.ServiceType == typeof(IConfiguration))
            ?.ImplementationInstance as IConfiguration;

        if (configuration is null)
            return false;

        connectionString = configuration.GetConnectionString("ApplicationInsights");
        return true;
    }

    private static string ResolveServiceName()
    {
        var envServiceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME");
        if (!string.IsNullOrWhiteSpace(envServiceName))
            return envServiceName.Trim();

        var assemblyServiceName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
        if (!string.IsNullOrWhiteSpace(assemblyServiceName))
            return assemblyServiceName;

        return "unknown_service:dotnet";
    }
}
