# MX.Observability.OpenTelemetry.AspNetCore

ASP.NET Core adapter for [`MX.Observability.OpenTelemetry`](https://www.nuget.org/packages/MX.Observability.OpenTelemetry).

Wires configurable filtering, audit logging and job telemetry into OpenTelemetry tracing/logging with Azure Monitor exporters.

## Usage

```csharp
using MX.Observability.OpenTelemetry.AspNetCore;

builder.Services.AddObservability();
```

Use this package in ASP.NET Core web/API apps.

For Worker Service / Azure Functions (isolated) hosts, use `MX.Observability.OpenTelemetry.WorkerService` instead.

See the [core package README](https://www.nuget.org/packages/MX.Observability.OpenTelemetry) for filter configuration, audit logging and job telemetry usage.
