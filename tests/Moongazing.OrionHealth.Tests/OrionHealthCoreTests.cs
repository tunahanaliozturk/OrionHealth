namespace Moongazing.OrionHealth.Tests;

using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Moongazing.OrionHealth;
using Moongazing.OrionHealth.DependencyInjection;
using Moongazing.OrionHealth.Diagnostics;

using Xunit;

public sealed class OrionHealthCoreTests
{
    private static ServiceProvider BuildWith(bool depHealthy)
    {
        var services = new ServiceCollection();
        services.AddLogging(); // a web host provides this; the HealthCheckService requires it
        services.AddOrionHealth()
            .AddLivenessCheck("self", () => HealthCheckResult.Healthy())
            .AddReadinessCheck("db", _ => Task.FromResult(depHealthy
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("connection refused")));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task A_failed_dependency_makes_readiness_unhealthy_but_leaves_liveness_healthy()
    {
        using var provider = BuildWith(depHealthy: false);
        var health = provider.GetRequiredService<HealthCheckService>();

        var readiness = await health.CheckHealthAsync(OrionHealthTags.IsReadiness);
        var liveness = await health.CheckHealthAsync(OrionHealthTags.IsLiveness);

        Assert.Equal(HealthStatus.Unhealthy, readiness.Status);  // dependency down -> not ready
        Assert.Equal(HealthStatus.Healthy, liveness.Status);     // process fine -> do NOT restart
    }

    [Fact]
    public async Task When_the_dependency_is_healthy_readiness_passes()
    {
        using var provider = BuildWith(depHealthy: true);
        var health = provider.GetRequiredService<HealthCheckService>();

        var readiness = await health.CheckHealthAsync(OrionHealthTags.IsReadiness);
        Assert.Equal(HealthStatus.Healthy, readiness.Status);
    }

    [Fact]
    public async Task Liveness_probe_never_runs_dependency_checks()
    {
        var ran = false;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOrionHealth()
            .AddLivenessCheck("self", () => HealthCheckResult.Healthy())
            .AddReadinessCheck("db", _ => { ran = true; return Task.FromResult(HealthCheckResult.Healthy()); });
        using var provider = services.BuildServiceProvider();
        var health = provider.GetRequiredService<HealthCheckService>();

        await health.CheckHealthAsync(OrionHealthTags.IsLiveness);
        Assert.False(ran); // the readiness (dependency) check must not run on the liveness path
    }

    [Fact]
    public async Task The_report_writer_emits_structured_json_with_the_trace_id()
    {
        using var provider = BuildWith(depHealthy: false);
        var health = provider.GetRequiredService<HealthCheckService>();
        var report = await health.CheckHealthAsync(OrionHealthTags.IsReadiness);

        var json = HealthReportWriter.Serialize(report, traceId: "abc123");

        Assert.Contains("\"status\":\"Unhealthy\"", json);
        Assert.Contains("\"db\":", json);
        Assert.Contains("\"durationMs\":", json);
        Assert.Contains("\"description\":\"connection refused\"", json); // Unhealthy(description) surfaces here
        Assert.Contains("\"traceId\":\"abc123\"", json);
    }

    [Fact]
    public async Task The_report_writer_omits_the_trace_id_when_absent()
    {
        using var provider = BuildWith(depHealthy: true);
        var health = provider.GetRequiredService<HealthCheckService>();
        var report = await health.CheckHealthAsync(OrionHealthTags.IsReadiness);

        var json = HealthReportWriter.Serialize(report, traceId: null);
        Assert.DoesNotContain("traceId", json);
    }

    [Fact]
    public async Task Recording_a_report_emits_a_duration_measurement_per_check()
    {
        using var diagnostics = new HealthDiagnostics();
        var count = 0;
        using var listener = new System.Diagnostics.Metrics.MeterListener
        {
            InstrumentPublished = (i, l) =>
            {
                if (Moongazing.Orion.Abstractions.Diagnostics.OrionInstrumentation.ListensTo(i, diagnostics))
                {
                    l.EnableMeasurementEvents(i);
                }
            },
        };
        listener.SetMeasurementEventCallback<double>((_, _, _, _) => Interlocked.Increment(ref count));
        listener.Start();

        using var provider = BuildWith(depHealthy: false);
        var health = provider.GetRequiredService<HealthCheckService>();
        var report = await health.CheckHealthAsync(OrionHealthTags.IsReadiness);
        diagnostics.RecordReport(report);

        Assert.Equal(1, count); // one readiness check -> one duration measurement
    }
}
