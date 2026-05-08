# Copilot Instructions

> Shared conventions: see [`.github-copilot/.github/instructions/dotnet-nuget-library.instructions.md`](../../.github-copilot/.github/instructions/dotnet-nuget-library.instructions.md) for general .NET NuGet library standards.

## Project Overview

This repository contains the MX OpenTelemetry observability libraries for .NET 9/10. It ships three NuGet packages:
- `MX.Observability.OpenTelemetry` (hosting-agnostic core)
- `MX.Observability.OpenTelemetry.AspNetCore` (ASP.NET Core adapter)
- `MX.Observability.OpenTelemetry.WorkerService` (Worker Service / Azure Functions isolated adapter)

## Architecture

- Solution: `src/MX.Observability.OpenTelemetry.sln`
- Core services are registered from `src/MX.Observability.OpenTelemetry/Extensions/ServiceCollectionExtensions.cs` via `AddObservabilityCore()`.
- Host adapters expose `AddObservability()` in:
  - `src/MX.Observability.OpenTelemetry.AspNetCore/ServiceCollectionExtensions.cs`
  - `src/MX.Observability.OpenTelemetry.WorkerService/ServiceCollectionExtensions.cs`
- Filtering options bind from `OpenTelemetry:Filtering` (`TelemetryFilterOptions.SectionName`) and include dependency, request, and log rules.
- Auditing primitives live under `src/MX.Observability.OpenTelemetry/Auditing/`; job telemetry abstractions and implementation live under `src/MX.Observability.OpenTelemetry/Jobs/`.

## Build and Test

- Build: `dotnet build src/MX.Observability.OpenTelemetry.sln`
- Test: `dotnet test src/MX.Observability.OpenTelemetry.sln`
- Package outputs are generated on build (`GeneratePackageOnBuild=true` in package projects).

## Conventions

- Target frameworks are `net9.0` and `net10.0` across package and test projects.
- Versioning is Nerdbank.GitVersioning from repo root `version.json`.
- Keep package README files in each package project folder aligned with package behavior (`PackageReadmeFile=README.md`).
- Prefer adding behavior in the core package and only host-pipeline wiring in adapter packages.
- Add or update tests in `src/MX.Observability.OpenTelemetry.Tests/` when changing filtering, auditing, or job telemetry logic.

## CI/CD

Workflows in `.github/workflows/` cover build and test, PR verification, code quality, release tagging, and NuGet publishing. Release publishing is split across `release-version-and-tag.yml` then `release-publish-nuget.yml`.
