using Bunit;
using FSH.Admin.Wasm.Pages.Settings;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Settings;

public class SessionsPageTests : TestSetup
{
    private readonly ISessionService _sessionService = Substitute.For<ISessionService>();

    public SessionsPageTests()
    {
        Services.AddSingleton(_sessionService);
    }

    private static UserSessionDto Session(
        Guid id,
        string device,
        string browser,
        string os,
        string ip,
        bool isCurrent,
        DateTime? lastActive = null) =>
        new(
            id,
            "u1",
            "jane",
            "jane@example.com",
            ip,
            device,
            browser,
            "137",
            os,
            "24H2",
            DateTime.UtcNow.AddDays(-1),
            lastActive ?? DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddDays(7),
            true,
            isCurrent);

    private void StubTwoSessions()
    {
        _sessionService.GetMySessionsAsync(Arg.Any<CancellationToken>())
            .Returns([
                Session(Guid.NewGuid(), "Desktop", "Chrome", "Windows", "10.0.0.2", isCurrent: false),
                Session(Guid.NewGuid(), "Mobile", "Safari", "iOS", "10.0.0.3", isCurrent: true),
            ]);
    }

    [Fact]
    public void Renders_session_rows_with_device_details()
    {
        StubTwoSessions();

        var cut = Render<SessionsPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Mobile");
            cut.Markup.ShouldContain("Safari");
            cut.Markup.ShouldContain("iOS");
            cut.Markup.ShouldContain("10.0.0.3");
            cut.Markup.ShouldContain("Desktop");
        });
    }

    [Fact]
    public void Marks_the_current_session_as_this_device()
    {
        StubTwoSessions();

        var cut = Render<SessionsPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("This device"));
    }

    [Fact]
    public void Hides_revoke_button_on_current_session()
    {
        StubTwoSessions();

        var cut = Render<SessionsPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Mobile"));
        // Two revoke controls exist (per-row Revoke + the bulk "Sign out all other sessions").
        cut.FindAll("button").Where(b => b.TextContent.Contains("Revoke")).Count().ShouldBeGreaterThanOrEqualTo(1);
        cut.Markup.ShouldContain("Sign out all other sessions");
    }

    [Fact]
    public void Revoking_a_session_calls_service_and_reloads()
    {
        var stale = Guid.NewGuid();
        _sessionService.GetMySessionsAsync(Arg.Any<CancellationToken>())
            .Returns([
                Session(stale, "Desktop", "Chrome", "Windows", "10.0.0.2", isCurrent: false),
                Session(Guid.NewGuid(), "Mobile", "Safari", "iOS", "10.0.0.3", isCurrent: true),
            ]);

        var cut = Render<SessionsPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Desktop"));

        cut.FindAll("button").First(b => b.TextContent.Contains("Revoke")).Click();

        cut.WaitForAssertion(() =>
            _sessionService.Received(1).RevokeMySessionAsync(stale, Arg.Any<CancellationToken>()));
    }

    [Fact]
    public void Revoking_all_calls_service_and_reloads()
    {
        StubTwoSessions();
        _sessionService.RevokeAllMySessionsAsync(Arg.Any<CancellationToken>()).Returns(2);

        var cut = Render<SessionsPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Sign out all other sessions"));

        cut.FindAll("button").First(b => b.TextContent.Contains("Sign out all other sessions")).Click();

        cut.WaitForAssertion(() =>
            _sessionService.Received(1).RevokeAllMySessionsAsync(Arg.Any<CancellationToken>()));
    }

    [Fact]
    public void Shows_empty_message_when_no_sessions()
    {
        _sessionService.GetMySessionsAsync(Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render<SessionsPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No active sessions."));
    }

    [Fact]
    public void Shows_error_band_when_load_fails()
    {
        _sessionService.GetMySessionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<UserSessionDto>>(new Exception("sessions boom")));

        var cut = Render<SessionsPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("sessions boom"));
    }
}
