# MX.Observability.OpenTelemetry

Hosting-agnostic core for the MX Observability libraries: configurable telemetry filter implementation, structured audit logging and scheduled job telemetry for .NET 9/10 applications using Azure OpenTelemetry.

**You normally do not reference this package directly.** Reference one of the host-specific adapter packages instead, which transitively reference this core package and additionally wire the telemetry filter into the correct SDK pipeline:

- [`MX.Observability.OpenTelemetry.AspNetCore`](https://www.nuget.org/packages/MX.Observability.OpenTelemetry.AspNetCore) — for hosts using `AddOpenTelemetryTelemetry()`
- [`MX.Observability.OpenTelemetry.WorkerService`](https://www.nuget.org/packages/MX.Observability.OpenTelemetry.WorkerService) — for hosts using `AddOpenTelemetryTelemetryWorkerService()`

See the [GitHub repository](https://github.com/frasermolyneux/observability-opentelemetry) for full documentation.

