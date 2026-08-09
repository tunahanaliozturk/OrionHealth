namespace Moongazing.OrionHealth.AspNetCore.Tests;

using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using Moongazing.OrionHealth.AspNetCore;
using Moongazing.OrionHealth.DependencyInjection;

using Xunit;

public sealed class MapOrionHealthTests
{
    private static Task<IHost> StartServerAsync(bool depHealthy) =>
        new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddOrionHealth()
                        .AddLivenessCheck("self", () => HealthCheckResult.Healthy())
                        .AddReadinessCheck("db", _ => Task.FromResult(depHealthy
                            ? HealthCheckResult.Healthy()
                            : HealthCheckResult.Unhealthy("connection refused")));
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapOrionHealth());
                });
            })
            .StartAsync();

    [Fact]
    public async Task A_failed_dependency_returns_503_from_readyz_while_healthz_stays_200()
    {
        using var host = await StartServerAsync(depHealthy: false);
        var client = host.GetTestClient();

        var liveness = await client.GetAsync("/healthz");
        var readiness = await client.GetAsync("/readyz");

        Assert.Equal(HttpStatusCode.OK, liveness.StatusCode);                 // process fine -> no restart
        Assert.Equal(HttpStatusCode.ServiceUnavailable, readiness.StatusCode); // dependency down -> drained

        var body = await readiness.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"Unhealthy\"", body);
        Assert.Contains("\"description\":\"connection refused\"", body);
        Assert.Contains("traceId", body);
        Assert.StartsWith("application/json", readiness.Content.Headers.ContentType!.ToString());
    }

    [Fact]
    public async Task A_healthy_dependency_returns_200_from_readyz()
    {
        using var host = await StartServerAsync(depHealthy: true);
        var client = host.GetTestClient();

        var readiness = await client.GetAsync("/readyz");
        Assert.Equal(HttpStatusCode.OK, readiness.StatusCode);

        var body = await readiness.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"Healthy\"", body);
    }

    [Fact]
    public async Task Healthz_returns_200_even_with_no_liveness_checks_and_a_failing_dependency()
    {
        using var host = await StartServerAsync(depHealthy: false);
        var client = host.GetTestClient();

        // Liveness runs only 'live'-tagged checks; the failing readiness dependency must not affect it.
        var liveness = await client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, liveness.StatusCode);
    }
}
