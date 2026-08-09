namespace Moongazing.OrionHealth;

using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// The tags that route a health check to the liveness or readiness endpoint, and the predicates that
/// select them. The split is the whole point: <see cref="Liveness"/> checks answer "is this process
/// wedged — restart it?" and must do no dependency I/O; <see cref="Readiness"/> checks answer "can
/// this pod serve traffic right now?" and probe dependencies. Wiring a readiness probe to a liveness
/// check turns a transient dependency blip into a crash loop — so keep them apart.
/// </summary>
public static class OrionHealthTags
{
    /// <summary>Tag for liveness checks (process responsive; no dependency I/O). Routed to <c>/healthz</c>.</summary>
    public const string Liveness = "live";

    /// <summary>Tag for readiness checks (dependency reachability). Routed to <c>/readyz</c>.</summary>
    public const string Readiness = "ready";

    /// <summary>Predicate selecting liveness checks.</summary>
    /// <param name="registration">The check registration.</param>
    /// <returns>True when the check is tagged <see cref="Liveness"/>.</returns>
    public static bool IsLiveness(HealthCheckRegistration registration)
    {
        System.ArgumentNullException.ThrowIfNull(registration);
        return registration.Tags.Contains(Liveness);
    }

    /// <summary>Predicate selecting readiness checks.</summary>
    /// <param name="registration">The check registration.</param>
    /// <returns>True when the check is tagged <see cref="Readiness"/>.</returns>
    public static bool IsReadiness(HealthCheckRegistration registration)
    {
        System.ArgumentNullException.ThrowIfNull(registration);
        return registration.Tags.Contains(Readiness);
    }
}
