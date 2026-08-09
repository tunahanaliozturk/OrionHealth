namespace Moongazing.OrionHealth;

using System;

/// <summary>
/// Health-probe configuration. The bounds ship as a stable option surface now; they are fully
/// enforced from Wave 2, when every probe runs under <see cref="ProbeTimeout"/> and shares a cached
/// result for <see cref="CacheDuration"/> so health checks never hang the probe endpoint or become a
/// load generator against a dependency.
/// </summary>
public sealed class OrionHealthOptions
{
    /// <summary>The per-probe timeout (enforced from Wave 2). Defaults to 2 seconds.</summary>
    public TimeSpan ProbeTimeout { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>How long a probe result is reused before re-running (enforced from Wave 2). Defaults to 5 seconds.</summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Validate the option values, throwing on an unusable configuration.</summary>
    public void Validate()
    {
        if (ProbeTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ProbeTimeout), ProbeTimeout, "ProbeTimeout must be positive.");
        }
        if (CacheDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(CacheDuration), CacheDuration, "CacheDuration cannot be negative.");
        }
    }
}
