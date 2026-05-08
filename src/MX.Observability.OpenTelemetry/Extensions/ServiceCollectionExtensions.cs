using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MX.Observability.OpenTelemetry.Auditing;
using MX.Observability.OpenTelemetry.Filtering.Configuration;
using MX.Observability.OpenTelemetry.Jobs;

namespace MX.Observability.OpenTelemetry.Extensions;

/// <summary>
/// Extension methods for registering MX Observability core services.
/// <para>
/// This package contains the hosting-agnostic pieces: filter options binding, the
/// <see cref="MX.Observability.OpenTelemetry.Filtering.TracingFilterProcessor"/> and
/// <see cref="MX.Observability.OpenTelemetry.Filtering.LogRecordFilterProcessor"/> implementations,
/// audit logging and job telemetry. Consumers should normally reference one of the host-specific
/// adapter packages (<c>MX.Observability.OpenTelemetry.AspNetCore</c> or
/// <c>MX.Observability.OpenTelemetry.WorkerService</c>) which call <see cref="AddObservabilityCore"/>
/// internally and additionally wire the telemetry processor into the correct SDK pipeline.
/// </para>
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the hosting-agnostic MX Observability services: telemetry filter options,
    /// audit logging and job telemetry. Does NOT wire the telemetry processor into the
    /// OpenTelemetry pipeline — use a host-specific adapter package for that.
    /// </summary>
    public static IServiceCollection AddObservabilityCore(this IServiceCollection services)
    {
        services.AddOptions<TelemetryFilterOptions>()
            .BindConfiguration(TelemetryFilterOptions.SectionName);
        
        services.AddSingleton<IValidateOptions<TelemetryFilterOptions>, TelemetryFilterOptionsValidator>();

        services.AddSingleton<IAuditLogger, OpenTelemetryAuditLogger>();
        services.AddSingleton<IJobTelemetry, OpenTelemetryJobTelemetry>();

        return services;
    }

    /// <summary>
    /// Registers the hosting-agnostic MX Observability services with custom filter configuration.
    /// </summary>
    public static IServiceCollection AddObservabilityCore(
        this IServiceCollection services,
        Action<TelemetryFilterOptions> configureFiltering)
    {
        services.AddOptions<TelemetryFilterOptions>()
            .BindConfiguration(TelemetryFilterOptions.SectionName)
            .Configure(configureFiltering);
        
        services.AddSingleton<IValidateOptions<TelemetryFilterOptions>, TelemetryFilterOptionsValidator>();

        services.AddSingleton<IAuditLogger, OpenTelemetryAuditLogger>();
        services.AddSingleton<IJobTelemetry, OpenTelemetryJobTelemetry>();

        return services;
    }

    /// <summary>
    /// Registers the audit logger for structured event emission.
    /// </summary>
    public static IServiceCollection AddAuditLogging(this IServiceCollection services)
    {
        services.AddSingleton<IAuditLogger, OpenTelemetryAuditLogger>();
        return services;
    }

    /// <summary>
    /// Registers the job telemetry service for scheduled job lifecycle tracking.
    /// </summary>
    public static IServiceCollection AddJobTelemetry(this IServiceCollection services)
    {
        services.AddSingleton<IJobTelemetry, OpenTelemetryJobTelemetry>();
        return services;
    }
}
