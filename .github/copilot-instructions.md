# Copilot Instructions

This repository publishes OpenTelemetry observability packages with Azure Monitor export, configurable filtering, structured auditing, and job lifecycle telemetry.

## Runtime and layout

- SDK: `10.0.301` from `global.json`; package and test projects target `net9.0` and `net10.0`.
- Solution: `src/MX.Observability.OpenTelemetry.slnx`.
- Core package: `MX.Observability.OpenTelemetry`.
- Adapters: `MX.Observability.OpenTelemetry.AspNetCore` and `MX.Observability.OpenTelemetry.WorkerService`.
- Tests: `MX.Observability.OpenTelemetry.Tests`.

## Repository rules

- Put hosting-agnostic filtering, auditing, and job telemetry behavior in the core package.
- Keep adapter packages focused on host pipeline, instrumentation, and exporter registration.
- Preserve `OpenTelemetry:Filtering` configuration, public registration methods, telemetry retention semantics, structured audit data, and job lifecycle behavior.
- Treat extension methods, options, auditing primitives, and job telemetry interfaces as public package contracts.
- Keep ASP.NET Core and Worker Service adapter boundaries distinct.
- Package IDs, target frameworks, package READMEs, generated package metadata, and NBGV configuration in `version.json` are release boundaries.
- Never add credentials or publish packages during routine validation.

## Validation

```pwsh
dotnet build src/MX.Observability.OpenTelemetry.slnx
dotnet test src/MX.Observability.OpenTelemetry.slnx
dotnet test src/MX.Observability.OpenTelemetry.slnx --filter "FullyQualifiedName~MyTestClass.MyTestMethod"
dotnet format src/MX.Observability.OpenTelemetry.slnx --verify-no-changes
```

Package roles and instrumentation boundaries are documented in `README.md` and `docs/README.md`.
