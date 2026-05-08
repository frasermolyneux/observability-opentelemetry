# MX.Observability.OpenTelemetry.WorkerService

Worker Service / Azure Functions (isolated) adapter for [`MX.Observability.OpenTelemetry`](https://www.nuget.org/packages/MX.Observability.OpenTelemetry).

Wires configurable filtering, audit logging and job telemetry into OpenTelemetry tracing/logging with Azure Monitor exporters.

## Usage

```csharp
using MX.Observability.OpenTelemetry.WorkerService;

builder.Services.AddObservability();
```

Use this package in Worker Services, console apps, and Azure Functions isolated worker hosts.

For ASP.NET Core hosts, use `MX.Observability.OpenTelemetry.AspNetCore` instead.

See the [core package README](https://www.nuget.org/packages/MX.Observability.OpenTelemetry) for filter configuration, audit logging and job telemetry usage.
