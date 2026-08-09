<!-- markdownlint-disable MD024 -->

# Changelog

All notable changes to OrionHealth are documented in this file. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-07-29

The first release — the Orion family's Wave 1 readiness foundation: the liveness/readiness split.

### Added

- **`OrionHealth`** (framework-free core):
  - **`OrionHealthTags`** — the `live` / `ready` tags and the `IsLiveness` / `IsReadiness` predicates
    that route a check to `/healthz` or `/readyz`.
  - **`AddOrionHealth`** — registers health checks, options, and diagnostics, returning the
    `IHealthChecksBuilder` for chaining; **`AddLivenessCheck`** / **`AddReadinessCheck`** register a
    check under the right tag.
  - **`HealthReportWriter`** — serializes a `HealthReport` to structured JSON (overall status,
    per-dependency status / duration / description / error, and a correlation `traceId`) through a
    source-gen `JsonSerializerContext` — reflection-free and AOT-clean.
  - **`OrionHealthOptions`** — `ProbeTimeout` / `CacheDuration` (a stable surface; fully enforced from
    Wave 2).
  - **OpenTelemetry by default** — `HealthDiagnostics` on the family's `OrionInstrumentation` spine: a
    `Moongazing.OrionHealth` meter with `orion.health.check.duration`, tagged by check and status.
  - Multi-targets `net8.0`/`net9.0`/`net10.0`; `IsAotCompatible`; a NativeAOT publish smoke test in CI.
- **`OrionHealth.AspNetCore`**:
  - **`MapOrionHealth()`** — maps `/healthz` (liveness — runs only `live`-tagged checks, no dependency
    I/O) and `/readyz` (readiness — runs `ready`-tagged checks), both writing the structured JSON with
    the request's `traceId`; readiness returns `503` when a dependency is unhealthy while liveness
    stays `200`. Composes with any existing `IHealthCheck`. References the ASP.NET Core shared
    framework (not NativeAOT-published; the core carries the AOT smoke).

### Scope

Wave 1 is the split and the endpoints. Deliberately deferred: bounded + cached probes via
OrionResilience/OrionClock and the first package probe bridges (W2); more bridges and richer `/health`
JSON (W3); suite-wide `AddOrionHealthAll()` auto-discovery, an OTel up/down gauge, and a Kubernetes
recipe (W4, GA).

### Verified

- Exit criteria met: an integration test (over a TestServer) asserts a failed dependency returns
  `503` from `/readyz` while `/healthz` stays `200` (no restart), and the reverse; the AOT smoke
  publishes trim/AOT-clean under `-warnaserror` and exits 0. 9 tests green across
  `net8.0`/`net9.0`/`net10.0`.
