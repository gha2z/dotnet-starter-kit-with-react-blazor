using Bunit;
using FSH.Admin.Wasm.Pages.Health;
using FSH.BlazorShared.Models.Health;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Health;

public class HealthPageTests : TestSetup
{
    private readonly IHealthService _healthService = Substitute.For<IHealthService>();

    public HealthPageTests()
    {
        Services.AddSingleton(_healthService);
    }

    private static HealthResult Live() => new()
    {
        Status = "Healthy",
        Results = [],
    };

    private static HealthResult Ready() => new()
    {
        Status = "Healthy",
        Results =
        [
            new HealthEntry { Name = "sqlserver", Status = "Healthy", Description = "database", DurationMs = 12.3, Details = new Dictionary<string, object> { ["latency"] = "2ms" } },
            new HealthEntry { Name = "redis", Status = "Healthy", Description = "cache", DurationMs = 3.1 },
        ],
    };

    private void StubHealthy()
    {
        _healthService.GetLivenessAsync(Arg.Any<CancellationToken>()).Returns(Live());
        _healthService.GetReadinessAsync(Arg.Any<CancellationToken>()).Returns(Ready());
    }

    [Fact]
    public void Renders_live_and_ready_probe_sections()
    {
        StubHealthy();

        var cut = Render<HealthPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Health"));
        cut.Markup.ShouldContain("/health/live");
        cut.Markup.ShouldContain("/health/ready");
        cut.Markup.ShouldContain("sqlserver");
        cut.Markup.ShouldContain("redis");
        cut.Markup.ShouldContain("12.3ms");
    }

    [Fact]
    public void Shows_stat_strip_totals()
    {
        StubHealthy();

        var cut = Render<HealthPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Liveness"));
        cut.Markup.ShouldContain("Readiness");
        cut.Markup.ShouldContain("Checks healthy");
        cut.Markup.ShouldContain("Checks failing");
        cut.Markup.ShouldContain("2");
    }

    [Fact]
    public void Shows_no_checks_message_when_liveness_has_no_results()
    {
        StubHealthy();

        var cut = Render<HealthPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No dependency checks reported."));
    }

    [Fact]
    public void Shows_error_band_when_probes_fail()
    {
        _healthService.GetLivenessAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<HealthResult>(new Exception("boom")));
        _healthService.GetReadinessAsync(Arg.Any<CancellationToken>())
            .Returns(Ready());

        var cut = Render<HealthPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Liveness probe failed: boom"));
    }

    [Fact]
    public void Degraded_readiness_counts_as_failing()
    {
        _healthService.GetLivenessAsync(Arg.Any<CancellationToken>()).Returns(Live());
        _healthService.GetReadinessAsync(Arg.Any<CancellationToken>())
            .Returns(new HealthResult
            {
                Status = "Degraded",
                Results =
                [
                    new HealthEntry { Name = "sqlserver", Status = "Healthy", DurationMs = 1 },
                    new HealthEntry { Name = "redis", Status = "Degraded", DurationMs = 1 },
                ],
            });

        var cut = Render<HealthPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("1 degraded · 0 unhealthy"));
    }

    [Fact]
    public void Expanding_a_check_row_reveals_details()
    {
        StubHealthy();

        var cut = Render<HealthPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("sqlserver"));

        cut.FindAll("div[role='button']").First().Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("LATENCY"));
    }
}
