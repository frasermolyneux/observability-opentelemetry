# observability-opentelemetry
[![Build and Test](https://github.com/frasermolyneux/observability-opentelemetry/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/frasermolyneux/observability-opentelemetry/actions/workflows/build-and-test.yml)
[![Code Quality](https://github.com/frasermolyneux/observability-opentelemetry/actions/workflows/codequality.yml/badge.svg)](https://github.com/frasermolyneux/observability-opentelemetry/actions/workflows/codequality.yml)
[![Copilot Setup Steps](https://github.com/frasermolyneux/observability-opentelemetry/actions/workflows/copilot-setup-steps.yml/badge.svg)](https://github.com/frasermolyneux/observability-opentelemetry/actions/workflows/copilot-setup-steps.yml)
[![Dependabot Auto-Merge](https://github.com/frasermolyneux/observability-opentelemetry/actions/workflows/dependabot-automerge.yml/badge.svg)](https://github.com/frasermolyneux/observability-opentelemetry/actions/workflows/dependabot-automerge.yml)
[![PR Verify](https://github.com/frasermolyneux/observability-opentelemetry/actions/workflows/pr-verify.yml/badge.svg)](https://github.com/frasermolyneux/observability-opentelemetry/actions/workflows/pr-verify.yml)
[![Release - Publish NuGet](https://github.com/frasermolyneux/observability-opentelemetry/actions/workflows/release-publish-nuget.yml/badge.svg)](https://github.com/frasermolyneux/observability-opentelemetry/actions/workflows/release-publish-nuget.yml)
[![Release - Version and Tag](https://github.com/frasermolyneux/observability-opentelemetry/actions/workflows/release-version-and-tag.yml/badge.svg)](https://github.com/frasermolyneux/observability-opentelemetry/actions/workflows/release-version-and-tag.yml)

## Documentation

* [Documentation Index](/docs/README.md) - Index of repository documentation and where to add future docs

## Overview

This repository contains the MX OpenTelemetry observability libraries for .NET 9 and .NET 10. It publishes three NuGet packages: a hosting-agnostic core package plus host-specific adapters for ASP.NET Core and Worker Service or Azure Functions isolated workloads. The libraries provide configurable telemetry filtering, structured audit logging, and scheduled job telemetry patterns over OpenTelemetry with Azure Monitor exporter integration. Unit tests validate filtering, auditing, and job telemetry behavior across the shared core package.

## NuGet Packages

| Package                                                                                                                       | Latest                                                                                                                                                                    | Description                                                                       |
| ----------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------- |
| [`MX.Observability.OpenTelemetry`](https://www.nuget.org/packages/MX.Observability.OpenTelemetry)                             | [![NuGet](https://img.shields.io/nuget/v/MX.Observability.OpenTelemetry.svg)](https://www.nuget.org/packages/MX.Observability.OpenTelemetry/)                             | Host-agnostic observability core with filtering, audit logging, and job telemetry |
| [`MX.Observability.OpenTelemetry.AspNetCore`](https://www.nuget.org/packages/MX.Observability.OpenTelemetry.AspNetCore)       | [![NuGet](https://img.shields.io/nuget/v/MX.Observability.OpenTelemetry.AspNetCore.svg)](https://www.nuget.org/packages/MX.Observability.OpenTelemetry.AspNetCore/)       | ASP.NET Core adapter for OpenTelemetry pipeline and exporter wiring               |
| [`MX.Observability.OpenTelemetry.WorkerService`](https://www.nuget.org/packages/MX.Observability.OpenTelemetry.WorkerService) | [![NuGet](https://img.shields.io/nuget/v/MX.Observability.OpenTelemetry.WorkerService.svg)](https://www.nuget.org/packages/MX.Observability.OpenTelemetry.WorkerService/) | Worker Service/Azure Functions isolated adapter for the observability stack       |

## Contributing

Please read the [contributing](CONTRIBUTING.md) guidance; this is a learning and development project.

## Security

Please read the [security](SECURITY.md) guidance; I am always open to security feedback through email or opening an issue.
