# MX.Observability.OpenTelemetry

Hosting-agnostic core for the MX Observability libraries: configurable telemetry filter implementation, structured audit logging and scheduled job telemetry for .NET 9/10 applications using Azure OpenTelemetry.

The core package also includes availability telemetry support via `IAvailabilityTelemetry`, which emits `microsoft.availability.*` dimensions through `ILogger` so Azure Monitor can map entries into Application Insights availability data.
This approach is based on the current Azure Monitor OpenTelemetry workaround discussed in `azure-sdk-for-net` issue `#46509` and may need revisiting when first-class SDK support ships.

`AvailabilityTelemetryEntry.Target` is an optional named target identifier reserved for custom implementations of `IAvailabilityTelemetry` that route entries across multiple Application Insights sinks. The default `OpenTelemetryAvailabilityTelemetry` shipped here ignores `Target` and emits to the host's primary sink.

**You normally do not reference this package directly.** Reference one of the host-specific adapter packages instead, which transitively reference this core package and additionally wire the telemetry filter into the correct SDK pipeline:

- [`MX.Observability.OpenTelemetry.AspNetCore`](https://www.nuget.org/packages/MX.Observability.OpenTelemetry.AspNetCore) — for ASP.NET Core hosts using `AddObservability()`
- [`MX.Observability.OpenTelemetry.WorkerService`](https://www.nuget.org/packages/MX.Observability.OpenTelemetry.WorkerService) — for Worker Service / Functions isolated hosts using `AddObservability()`

See the [GitHub repository](https://github.com/frasermolyneux/observability-opentelemetry) for full documentation.

