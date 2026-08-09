namespace Moongazing.OrionHealth;

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Serializes a <see cref="HealthReport"/> to the family's structured JSON: an overall status plus a
/// per-dependency breakdown (status, duration, and error when unhealthy), and the correlation
/// <c>traceId</c> so the readiness response and the failing dependency's trace share one id. Uses a
/// source-gen <see cref="JsonSerializerContext"/>, so it is reflection-free and AOT-clean.
/// </summary>
public static class HealthReportWriter
{
    /// <summary>Serialize <paramref name="report"/> to the structured health JSON.</summary>
    /// <param name="report">The health report.</param>
    /// <param name="traceId">The correlation id to include, or null to omit.</param>
    /// <returns>The JSON payload.</returns>
    public static string Serialize(HealthReport report, string? traceId = null)
    {
        System.ArgumentNullException.ThrowIfNull(report);

        var results = new Dictionary<string, HealthEntryDto>(report.Entries.Count, System.StringComparer.Ordinal);
        foreach (var (name, entry) in report.Entries)
        {
            results[name] = new HealthEntryDto(
                Status: entry.Status.ToString(),
                DurationMs: entry.Duration.TotalMilliseconds,
                Description: entry.Description,
                Error: entry.Exception?.Message);
        }

        var dto = new HealthReportDto(report.Status.ToString(), results, traceId);
        return JsonSerializer.Serialize(dto, OrionHealthJsonContext.Default.HealthReportDto);
    }
}

/// <summary>The serialized overall health report.</summary>
/// <param name="Status">The overall status: <c>Healthy</c> / <c>Degraded</c> / <c>Unhealthy</c>.</param>
/// <param name="Results">Per-dependency results, keyed by check name.</param>
/// <param name="TraceId">The correlation id, or null.</param>
public sealed record HealthReportDto(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("results")] IReadOnlyDictionary<string, HealthEntryDto> Results,
    [property: JsonPropertyName("traceId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? TraceId);

/// <summary>One dependency's health result.</summary>
/// <param name="Status">The check status.</param>
/// <param name="DurationMs">How long the check took, in milliseconds.</param>
/// <param name="Description">An optional human-readable description.</param>
/// <param name="Error">The failure message when unhealthy, or null.</param>
public sealed record HealthEntryDto(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("durationMs")] double DurationMs,
    [property: JsonPropertyName("description"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Description,
    [property: JsonPropertyName("error"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Error);

/// <summary>Source-gen JSON context for the health report types (reflection-free, AOT-clean).</summary>
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(HealthReportDto))]
public sealed partial class OrionHealthJsonContext : JsonSerializerContext
{
}
