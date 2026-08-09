namespace Moongazing.OrionHealth.AspNetCore;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Moongazing.OrionHealth;
using Moongazing.OrionHealth.Diagnostics;

/// <summary>
/// Maps the OrionHealth probe endpoints.
/// </summary>
public static class OrionHealthEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Map <c>/healthz</c> (liveness — runs only <see cref="OrionHealthTags.Liveness"/> checks, no
    /// dependency I/O) and <c>/readyz</c> (readiness — runs <see cref="OrionHealthTags.Readiness"/>
    /// checks). Both return the structured health JSON with a correlation <c>traceId</c>; readiness
    /// returns <c>503</c> when a dependency is unhealthy while liveness stays <c>200</c>, so a
    /// dependency blip drains the pod from the service instead of restarting it.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="livenessPath">The liveness path. Defaults to <c>/healthz</c>.</param>
    /// <param name="readinessPath">The readiness path. Defaults to <c>/readyz</c>.</param>
    /// <returns>The same <paramref name="endpoints"/>, for chaining.</returns>
    public static IEndpointRouteBuilder MapOrionHealth(this IEndpointRouteBuilder endpoints, string livenessPath = "/healthz", string readinessPath = "/readyz")
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrEmpty(livenessPath);
        ArgumentException.ThrowIfNullOrEmpty(readinessPath);

        endpoints.MapHealthChecks(livenessPath, new HealthCheckOptions
        {
            Predicate = OrionHealthTags.IsLiveness,
            ResponseWriter = WriteResponseAsync,
        });
        endpoints.MapHealthChecks(readinessPath, new HealthCheckOptions
        {
            Predicate = OrionHealthTags.IsReadiness,
            ResponseWriter = WriteResponseAsync,
        });
        return endpoints;
    }

    private static Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        var diagnostics = context.RequestServices.GetService<HealthDiagnostics>() ?? HealthDiagnostics.Shared;
        diagnostics.RecordReport(report);

        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        var json = HealthReportWriter.Serialize(report, traceId);
        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsync(json);
    }
}
