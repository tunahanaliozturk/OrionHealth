# OrionHealth.AspNetCore

ASP.NET Core endpoints for OrionHealth: `MapOrionHealth()` maps `/healthz` (liveness, no dependency I/O) and `/readyz` (readiness, dependency probes), so Kubernetes drains a pod whose dependency died instead of restarting it.

![What /readyz and /healthz do on each probe: readiness answers 503 when a ready-tagged check is Unhealthy, so the pod is drained; liveness runs only live-tagged checks, so a dependency outage never restarts the pod](https://raw.githubusercontent.com/tunahanaliozturk/OrionHealth/master/docs/diagrams/probe-flow.png)

## Install

    dotnet add package OrionHealth.AspNetCore

It brings in the `OrionHealth` core package, where the checks are registered.

## Quick start

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moongazing.OrionHealth.AspNetCore;
using Moongazing.OrionHealth.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOrionHealth()
    .AddLivenessCheck("self", () => HealthCheckResult.Healthy())  // gates /healthz
    .AddReadinessCheck("db", async _ =>                            // gates /readyz
        await CanReachDatabaseAsync()
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("connection refused"));

var app = builder.Build();
app.MapOrionHealth(); // or MapOrionHealth("/live", "/ready")
app.Run();
```

`CanReachDatabaseAsync` stands for your own cheap reachability check.

## Behaviour

- `/healthz` runs only checks tagged `live`. It answers 503 only when one of them is `Unhealthy`; with no liveness checks it always answers 200.
- `/readyz` runs only checks tagged `ready`. It answers 200 when all are `Healthy` or `Degraded`, and 503 when any is `Unhealthy` or throws.
- Both write `application/json`: overall `status`, per-check `status`, `durationMs`, `description` and `error`, and a `traceId` (the current `Activity` trace id, else `HttpContext.TraceIdentifier`).
- Each response records `orion.health.check.duration` on the `Moongazing.OrionHealth` meter, through the `HealthDiagnostics` registered by `AddOrionHealth` (or `HealthDiagnostics.Shared`).

## AOT

This package references the ASP.NET Core shared framework and is not marked `IsAotCompatible`; the `OrionHealth` core is the AOT-checked part. Targets net8.0, net9.0 and net10.0.

## Related packages

- `OrionHealth` - the tags, the `AddOrionHealth` / `AddLivenessCheck` / `AddReadinessCheck` registration helpers and the JSON writer.

## Links

- Documentation and full README: https://github.com/tunahanaliozturk/OrionHealth
- Changelog: https://github.com/tunahanaliozturk/OrionHealth/blob/master/CHANGELOG.md
- License: MIT
