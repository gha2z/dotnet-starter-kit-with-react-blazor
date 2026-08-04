using Bunit;
using FSH.BlazorShared.Models.Health;
using FSH.BlazorShared.Services;
using FSH.Dashboard.Wasm.Pages.SystemPages;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.System;

public sealed class HealthPageTests : TestSetup
{
    private readonly IHealthService _healthService = Substitute.For<IHealthService>();

    public HealthPageTests()
    {
        Services.AddSingleton(_healthService);
    }

    [Fact]
    public void Renders_header_and_probe_sections()
    {
        _healthService.GetLivenessAsync(Arg.Any<CancellationToken>())
            .Returns(new HealthResult { Status = "Healthy", Results = [] });
        _healthService.GetReadinessAsync(Arg.Any<CancellationToken>())
            .Returns(new HealthResult { Status = "Healthy", Results = [] });

        var cut = Render<HealthPage>();

        cut.Markup.ShouldContain("Health");
        cut.Markup.ShouldContain("/health/live");
        cut.Markup.ShouldContain("/health/ready");
    }

    [Fact]
    public void Shows_Healthy_status_when_probes_return_healthy()
    {
        _healthService.GetLivenessAsync(Arg.Any<CancellationToken>())
            .Returns(new HealthResult { Status = "Healthy", Results = [] });
        _healthService.GetReadinessAsync(Arg.Any<CancellationToken>())
            .Returns(new HealthResult { Status = "Healthy", Results = [] });

        var cut = Render<HealthPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Healthy"));
    }

    [Fact]
    public void Shows_check_count_when_ready_has_results()
    {
        _healthService.GetLivenessAsync(Arg.Any<CancellationToken>())
            .Returns(new HealthResult { Status = "Healthy", Results = [] });
        _healthService.GetReadinessAsync(Arg.Any<CancellationToken>())
            .Returns(new HealthResult
            {
                Status = "Healthy",
                Results =
                [
                    new HealthEntry { Name = "Postgres", Status = "Healthy", DurationMs = 12.5 },
                    new HealthEntry { Name = "Redis", Status = "Healthy", DurationMs = 3.2 },
                ]
            });

        var cut = Render<HealthPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Postgres"));
        cut.Markup.ShouldContain("Redis");
        cut.Markup.ShouldContain("2");
    }

    [Fact]
    public void Shows_error_band_when_probe_throws()
    {
        _healthService.GetLivenessAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<HealthResult>(new Exception("connection refused")));
        _healthService.GetReadinessAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<HealthResult>(new Exception("timeout")));

        var cut = Render<HealthPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Liveness probe failed"));
        cut.Markup.ShouldContain("Readiness probe failed");
    }
}
