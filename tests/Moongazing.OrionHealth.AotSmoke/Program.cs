// NativeAOT smoke test for the OrionHealth core. Publishing this with PublishAot=true must produce
// zero trim/AOT warnings, and running it must exit 0 - that pair is the core's AOT exit criterion.
// It exercises the tag predicates, the source-gen health-report JSON writer, and telemetry. The
// ASP.NET Core endpoint mapping lives in OrionHealth.AspNetCore and is not part of the AOT surface.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using Moongazing.OrionHealth;
using Moongazing.OrionHealth.Diagnostics;

// Tag predicates route a registration correctly.
var readinessReg = new HealthCheckRegistration("db", new AlwaysHealthy(), failureStatus: null, tags: new[] { OrionHealthTags.Readiness });
var livenessReg = new HealthCheckRegistration("self", new AlwaysHealthy(), failureStatus: null, tags: new[] { OrionHealthTags.Liveness });
Check(OrionHealthTags.IsReadiness(readinessReg) && !OrionHealthTags.IsLiveness(readinessReg), "readiness predicate wrong");
Check(OrionHealthTags.IsLiveness(livenessReg) && !OrionHealthTags.IsReadiness(livenessReg), "liveness predicate wrong");

// Build a report by hand and serialize it through the source-gen writer.
var entries = new Dictionary<string, HealthReportEntry>(StringComparer.Ordinal)
{
    ["cache"] = new HealthReportEntry(HealthStatus.Healthy, description: null, duration: TimeSpan.FromMilliseconds(3), exception: null, data: null),
    ["db"] = new HealthReportEntry(HealthStatus.Unhealthy, description: "connection refused", duration: TimeSpan.FromMilliseconds(2001), exception: null, data: null),
};
var report = new HealthReport(entries, TimeSpan.FromMilliseconds(2004));

var json = HealthReportWriter.Serialize(report, traceId: "a1b2");
Check(json.Contains("\"status\":\"Unhealthy\"") && json.Contains("\"traceId\":\"a1b2\"") && json.Contains("connection refused"), "report json wrong");

// Telemetry records a measurement per entry.
using var diagnostics = new HealthDiagnostics();
Check(diagnostics.Meter.Name == HealthDiagnostics.MeterName, "meter name wrong");
diagnostics.RecordReport(report);

Console.WriteLine("OrionHealth AOT smoke test passed.");
return 0;

static void Check(bool condition, string message)
{
    if (!condition)
    {
        Console.Error.WriteLine($"AOT smoke test failed: {message}");
        Environment.Exit(1);
    }
}

internal sealed class AlwaysHealthy : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(HealthCheckResult.Healthy());
}
