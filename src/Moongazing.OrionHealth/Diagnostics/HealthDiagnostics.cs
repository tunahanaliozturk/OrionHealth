namespace Moongazing.OrionHealth.Diagnostics;

using System.Collections.Generic;
using System.Diagnostics.Metrics;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using Moongazing.Orion.Abstractions.Diagnostics;

/// <summary>
/// OpenTelemetry instrumentation for health probing. Built on the Orion family's
/// <see cref="OrionInstrumentation"/> spine: a <see cref="Meter"/> named <c>Moongazing.OrionHealth</c>
/// (subscribe by that name) carrying <c>orion.health.check.duration</c> (a per-check duration
/// histogram in milliseconds, tagged with the check name and its status) so dashboards show the same
/// up/down signal Kubernetes acts on. Multi-tenant / multi-region labels configured through
/// <see cref="OrionInstrumentation.SetStaticTags"/> are stamped onto every measurement.
/// <para>A process-wide <see cref="Shared"/> instance makes telemetry emit by default.</para>
/// </summary>
public sealed class HealthDiagnostics : OrionInstrumentation
{
    /// <summary>The meter name OpenTelemetry consumers subscribe to.</summary>
    public const string MeterName = "Moongazing.OrionHealth";

    /// <summary>The tag key carrying the check name.</summary>
    public const string CheckTagKey = "orion.health.check";

    /// <summary>The tag key carrying the check status.</summary>
    public const string StatusTagKey = "orion.health.status";

    private static readonly System.Lazy<HealthDiagnostics> SharedInstance =
        new(static () => new HealthDiagnostics());

    /// <summary>Create the meter and its instrument.</summary>
    public HealthDiagnostics()
        : base(OrionTelemetry.ScopeName("OrionHealth"), MeterVersion.Value)
    {
        CheckDuration = Meter.CreateHistogram<double>(
            OrionTelemetry.MetricName("health", "check.duration"),
            unit: "ms",
            description: "Per-check probe duration, tagged with the check name and status.");
    }

    /// <summary>The process-wide default instance, so telemetry emits without explicit wiring.</summary>
    public static HealthDiagnostics Shared => SharedInstance.Value;

    /// <summary>Records each check's probe duration.</summary>
    public Histogram<double> CheckDuration { get; }

    /// <summary>Record every entry of a completed <paramref name="report"/>.</summary>
    /// <param name="report">The health report to record.</param>
    public void RecordReport(HealthReport report)
    {
        System.ArgumentNullException.ThrowIfNull(report);
        foreach (var (name, entry) in report.Entries)
        {
            CheckDuration.Record(
                entry.Duration.TotalMilliseconds,
                new KeyValuePair<string, object?>(CheckTagKey, name),
                new KeyValuePair<string, object?>(StatusTagKey, entry.Status.ToString()));
        }
    }
}
