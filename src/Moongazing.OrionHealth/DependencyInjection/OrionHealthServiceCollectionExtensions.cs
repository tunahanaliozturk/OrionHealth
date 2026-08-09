namespace Moongazing.OrionHealth.DependencyInjection;

using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Moongazing.OrionHealth;
using Moongazing.OrionHealth.Diagnostics;

/// <summary>
/// DI wiring for health probing.
/// </summary>
public static class OrionHealthServiceCollectionExtensions
{
    /// <summary>
    /// Register health checks, the OrionHealth options, and the shared <see cref="HealthDiagnostics"/>,
    /// returning the <see cref="IHealthChecksBuilder"/> so probes chain on
    /// (<c>.AddReadinessCheck(...)</c>, package probe bridges, or plain <c>.AddCheck(...)</c>).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration of the probe options.</param>
    /// <returns>The health-checks builder, for chaining.</returns>
    public static IHealthChecksBuilder AddOrionHealth(this IServiceCollection services, Action<OrionHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<OrionHealthOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }
        optionsBuilder.PostConfigure(static o => o.Validate());

        services.TryAddSingleton<HealthDiagnostics>();
        return services.AddHealthChecks();
    }

    /// <summary>Register a readiness check (tagged <see cref="OrionHealthTags.Readiness"/>) — probes a dependency; gates <c>/readyz</c>.</summary>
    /// <param name="builder">The health-checks builder.</param>
    /// <param name="name">The check name.</param>
    /// <param name="check">The check delegate.</param>
    /// <returns>The same <paramref name="builder"/>, for chaining.</returns>
    public static IHealthChecksBuilder AddReadinessCheck(this IHealthChecksBuilder builder, string name, Func<CancellationToken, Task<HealthCheckResult>> check)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(check);
        return builder.AddAsyncCheck(name, () => check(default), tags: new[] { OrionHealthTags.Readiness });
    }

    /// <summary>Register a liveness check (tagged <see cref="OrionHealthTags.Liveness"/>) — process-only, no dependency I/O; gates <c>/healthz</c>.</summary>
    /// <param name="builder">The health-checks builder.</param>
    /// <param name="name">The check name.</param>
    /// <param name="check">The check delegate.</param>
    /// <returns>The same <paramref name="builder"/>, for chaining.</returns>
    public static IHealthChecksBuilder AddLivenessCheck(this IHealthChecksBuilder builder, string name, Func<HealthCheckResult> check)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(check);
        return builder.AddCheck(name, new DelegateHealthCheck(check), tags: new[] { OrionHealthTags.Liveness });
    }

    private sealed class DelegateHealthCheck(Func<HealthCheckResult> check) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(check());
    }
}
