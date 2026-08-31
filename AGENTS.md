# observability-opentelemetry

Multi-target .NET observability libraries built on OpenTelemetry with Azure Monitor export. The repository publishes a hosting-agnostic core package and ASP.NET Core and Worker Service adapters.

## Locations

- Solution: `src/MX.Observability.OpenTelemetry.sln`
- Core package: `src/MX.Observability.OpenTelemetry`
- Host adapters: `src/MX.Observability.OpenTelemetry.AspNetCore`, `src/MX.Observability.OpenTelemetry.WorkerService`
- Tests: `src/MX.Observability.OpenTelemetry.Tests`
- Documentation: `README.md` and `docs/`

## Commands

```pwsh
dotnet build src/MX.Observability.OpenTelemetry.sln
dotnet test src/MX.Observability.OpenTelemetry.sln
dotnet test src/MX.Observability.OpenTelemetry.sln --filter "FullyQualifiedName~MyTestClass.MyTestMethod"
dotnet format src/MX.Observability.OpenTelemetry.sln --verify-no-changes
```

## Constraints

- Keep filtering, auditing, and job telemetry behavior in the core package; adapters should contain host-pipeline and exporter wiring only.
- Preserve configuration sections, public registration methods, instrumentation behavior, and consumer-visible telemetry semantics.
- Keep ASP.NET Core and Worker Service package boundaries distinct.
- Keep package identities, target frameworks, package READMEs, and `version.json` behavior unchanged unless explicitly requested.
- Build generates packages; do not publish them during validation.

## Documentation

- [Package overview](README.md)
- [Documentation index](docs/README.md)
