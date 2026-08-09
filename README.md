<p align="center">
  <img src="docs/logo.png" alt="OrionHealth" width="150" />
</p>

# OrionHealth

[![CI/CD](https://github.com/tunahanaliozturk/OrionHealth/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/tunahanaliozturk/OrionHealth/actions/workflows/ci-cd.yml)
[![NuGet](https://img.shields.io/nuget/v/OrionHealth.svg)](https://www.nuget.org/packages/OrionHealth/)

**The suite's readiness story in one place.** An opinionated liveness/readiness split over `Microsoft.Extensions.Diagnostics.HealthChecks`: `/healthz` answers "is this process wedged — restart it?" (no dependency I/O) and `/readyz` answers "can this pod serve traffic right now?" (dependency probes) — so Kubernetes drains a pod whose database or Redis just died instead of crash-looping it on a transient blip.

Kubernetes needs two truthful signals, and conflating them is a production outage in either direction: a readiness probe wired to a liveness check *restarts* a pod on every downstream hiccup; a liveness probe that pings the database keeps a healthy-but-isolated pod alive while it can't work. `AspNetCore.Diagnostics.HealthChecks` is good plumbing but a blank framework — it doesn't ship the split, doesn't know your libraries, and doesn't tie health into your telemetry. OrionHealth ships the split as the default and emits health as the same OTel signal the rest of the suite uses.

## Packages

- **`OrionHealth`** — the framework-free core: the readiness/liveness tags and predicates, tagged registration helpers, a structured health-report JSON writer, and OpenTelemetry. AOT-clean.
- **`OrionHealth.AspNetCore`** — `MapOrionHealth()`, which maps `/healthz` + `/readyz`.

## Install

```bash
dotnet add package OrionHealth.AspNetCore
```

## Usage

```csharp
using Moongazing.OrionHealth.DependencyInjection;
using Moongazing.OrionHealth.AspNetCore;

builder.Services.AddOrionHealth()
    .AddLivenessCheck("self", () => HealthCheckResult.Healthy())          // process-only, gates /healthz
    .AddReadinessCheck("db", async ct => await PingDatabaseAsync(ct))     // dependency, gates /readyz
    .AddCheck("custom", () => HealthCheckResult.Healthy(), tags: ["ready"]); // plain checks compose

var app = builder.Build();
app.MapOrionHealth();   // maps /healthz (liveness) + /readyz (readiness)
```

- `/healthz` → **200** as long as the process is responsive (runs only liveness-tagged checks; no dependency I/O).
- `/readyz` → **200** only if all readiness-tagged probes pass; otherwise **503** with per-dependency detail:

```jsonc
{
  "status": "Unhealthy",
  "results": {
    "cache": { "status": "Healthy",   "durationMs": 3 },
    "db":    { "status": "Unhealthy", "durationMs": 2001, "description": "connection refused" }
  },
  "traceId": "a1b2..."
}
```

## Observability

A `Moongazing.OrionHealth` meter records `orion.health.check.duration` (a per-check histogram in milliseconds, tagged with the check name and status), so a dashboard shows the same up/down signal Kubernetes acts on. The readiness response carries a `traceId` linking it to the failing dependency's trace.

## Roadmap

This is the **Wave 1** foundation (v0.1): the liveness/readiness split, tag routing, the structured JSON writer, and telemetry. Later waves add bounded + cached probes (every check runs under `ProbeTimeout`; concurrent probes share a result for `CacheDuration`, so health checks never hang the endpoint or become a load generator) via [OrionResilience](https://github.com/tunahanaliozturk/OrionResilience)/[OrionClock](https://github.com/tunahanaliozturk/OrionClock) and the first package probe bridges (W2); more bridges — [OrionCache](https://github.com/tunahanaliozturk/OrionCache), [OrionRelay](https://github.com/tunahanaliozturk/OrionRelay), [OrionFlag](https://github.com/tunahanaliozturk/OrionFlag) — and richer `/health` JSON (W3); and suite-wide `AddOrionHealthAll()` auto-discovery, an OTel up/down gauge, and a Kubernetes recipe (W4, GA). See [CHANGELOG.md](CHANGELOG.md).

OrionHealth composes with `IHealthCheck`, so any existing custom check keeps working. It is not a monitoring/alerting system (point Prometheus/Grafana at the endpoints), not a health *dashboard UI* (structured JSON only), and runs cheap reachability probes — no deep/expensive diagnostic checks on the readiness path.

## Versioning

Follows [Semantic Versioning](https://semver.org/). Multi-targets `net8.0`, `net9.0`, and `net10.0`. Binds to `Orion.Abstractions` 1.x; the core is AOT- and trim-clean (verified by a native-binary smoke test), while `OrionHealth.AspNetCore` references the ASP.NET Core shared framework.

## Documentation

- [CHANGELOG.md](CHANGELOG.md) — release notes.

## Contributing

Contributions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) and the [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## More from the Orion family

Focused .NET libraries built to one quality bar. Each is usable on its own; several share the small [`Orion.Abstractions`](https://github.com/tunahanaliozturk/Orion.Abstractions) contracts spine, but there is no deep dependency web — pick only what you need:

- [Orion.Abstractions](https://github.com/tunahanaliozturk/Orion.Abstractions) — the shared contracts spine: telemetry, options, result, clock
- [OrionClock](https://github.com/tunahanaliozturk/OrionClock) — a `TimeProvider`-based clock with TTL / deadline vocabulary
- [OrionResult](https://github.com/tunahanaliozturk/OrionResult) — Result/Option types and a shared error vocabulary
- [OrionCache](https://github.com/tunahanaliozturk/OrionCache) — cache-aside with single-flight stampede protection
- [OrionResilience](https://github.com/tunahanaliozturk/OrionResilience) — retry, backoff, and timeout on OrionClock
- [OrionRate](https://github.com/tunahanaliozturk/OrionRate) — rate limiting on OrionClock
- [OrionFlag](https://github.com/tunahanaliozturk/OrionFlag) — in-process feature flags
- [OrionPage](https://github.com/tunahanaliozturk/OrionPage) — keyset/cursor pagination for EF Core
- [OrionEnvelope](https://github.com/tunahanaliozturk/OrionEnvelope) — one HTTP contract: envelope + problem+json
- [OrionGuard](https://github.com/tunahanaliozturk/OrionGuard) — validation, guard clauses, DDD primitives, domain events
- [OrionAudit](https://github.com/tunahanaliozturk/OrionAudit) — automatic EF Core change-audit trail
- [OrionBeacon](https://github.com/tunahanaliozturk/OrionBeacon) — leader election with fencing tokens
- [OrionGrant](https://github.com/tunahanaliozturk/OrionGrant) — permission / authorization checks
- [OrionInbox](https://github.com/tunahanaliozturk/OrionInbox) — transactional inbox for exactly-once effects
- [OrionKey](https://github.com/tunahanaliozturk/OrionKey) — source-generated strongly-typed IDs
- [OrionLedger](https://github.com/tunahanaliozturk/OrionLedger) — API-key issuance, verification, and rotation
- [OrionLens](https://github.com/tunahanaliozturk/OrionLens) — ambient correlation-context propagation
- [OrionLock](https://github.com/tunahanaliozturk/OrionLock) — distributed locks with fencing tokens
- [OrionOnce](https://github.com/tunahanaliozturk/OrionOnce) — idempotency keys for exactly-once request handling
- [OrionPatch](https://github.com/tunahanaliozturk/OrionPatch) — transactional outbox for EF Core
- [OrionRelay](https://github.com/tunahanaliozturk/OrionRelay) — outbound webhook delivery (HMAC, retries, backoff)
- [OrionSaga](https://github.com/tunahanaliozturk/OrionSaga) — sagas / process managers for long-running workflows
- [OrionShade](https://github.com/tunahanaliozturk/OrionShade) — sensitive-data redaction for logs and telemetry
- [OrionStream](https://github.com/tunahanaliozturk/OrionStream) — server-sent events / streaming hub
- [OrionVault](https://github.com/tunahanaliozturk/OrionVault) — field-level encryption for EF Core

See it all working together in [OrionShowcase](https://github.com/tunahanaliozturk/OrionShowcase), a production-shaped banking sample.

## License

[MIT](LICENSE).
