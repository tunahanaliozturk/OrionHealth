# OrionHealth

The framework-free core of OrionHealth: liveness and readiness tags, tagged registration helpers, a structured health-report JSON writer and OpenTelemetry, over `Microsoft.Extensions.Diagnostics.HealthChecks`. For the `/healthz` and `/readyz` endpoints, add `OrionHealth.AspNetCore`.

![Where the OrionHealth core sits: AddOrionHealth registers tagged checks over ASP.NET Core health checks, OrionHealth.AspNetCore maps the endpoints, and the core emits orion.health.check.duration](https://raw.githubusercontent.com/tunahanaliozturk/OrionHealth/master/docs/diagrams/overview.png)

## Install

    dotnet add package OrionHealth

## Quick start

```csharp
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moongazing.OrionHealth;
using Moongazing.OrionHealth.DependencyInjection;
using Moongazing.OrionHealth.Diagnostics;

var services = new ServiceCollection();
services.AddLogging(); // a host provides this; HealthCheckService needs it
services.AddOrionHealth()
    .AddLivenessCheck("self", () => HealthCheckResult.Healthy())         // tag "live": no dependency I/O
    .AddReadinessCheck("db", _ => Task.FromResult(HealthCheckResult.Healthy())); // tag "ready": dependency probe

using var provider = services.BuildServiceProvider();
var health = provider.GetRequiredService<HealthCheckService>();

HealthReport readiness = await health.CheckHealthAsync(OrionHealthTags.IsReadiness);
provider.GetRequiredService<HealthDiagnostics>().RecordReport(readiness); // orion.health.check.duration
string json = HealthReportWriter.Serialize(readiness, Activity.Current?.TraceId.ToString());
```

Plain `AddCheck(..., tags: [OrionHealthTags.Readiness])` registrations and any existing `IHealthCheck` take part as well; the tag decides which probe runs them.

## What it gives you

- `OrionHealthTags.Liveness` (`"live"`) and `OrionHealthTags.Readiness` (`"ready"`), with the `IsLiveness` / `IsReadiness` predicates.
- `AddOrionHealth(configure?)` registers health checks, `OrionHealthOptions` and a `HealthDiagnostics` singleton, and returns the `IHealthChecksBuilder`.
- `AddLivenessCheck(name, Func<HealthCheckResult>)` and `AddReadinessCheck(name, Func<CancellationToken, Task<HealthCheckResult>>)`. In 0.1.0 the readiness delegate receives `CancellationToken.None`.
- `HealthReportWriter.Serialize(report, traceId)` writes `{ "status", "results": { name: { "status", "durationMs", "description", "error" } }, "traceId" }`; null fields are left out. It uses a source-generated `JsonSerializerContext`.

## Options

- `ProbeTimeout` - default 2 seconds, must be positive.
- `CacheDuration` - default 5 seconds, must not be negative.

In 0.1.0 nothing reads them yet: they are validated only when your code resolves `IOptions<OrionHealthOptions>`, and enforced once bounded and cached probes arrive in a later wave.

## Telemetry and AOT

- Meter `Moongazing.OrionHealth`, histogram `orion.health.check.duration` in milliseconds, tagged `orion.health.check` (check name) and `orion.health.status`. `HealthDiagnostics.Shared` is the process-wide default instance.
- AOT- and trim-compatible (`IsAotCompatible`), checked by a NativeAOT smoke test in CI. Targets net8.0, net9.0 and net10.0.

## Related packages

- `OrionHealth.AspNetCore` - `MapOrionHealth()` maps `/healthz` and `/readyz` with this JSON and telemetry.
- `Orion.Abstractions` - the family contracts; `HealthDiagnostics` is built on its `OrionInstrumentation`.

## Links

- Documentation and full README: https://github.com/tunahanaliozturk/OrionHealth
- Changelog: https://github.com/tunahanaliozturk/OrionHealth/blob/master/CHANGELOG.md
- License: MIT
