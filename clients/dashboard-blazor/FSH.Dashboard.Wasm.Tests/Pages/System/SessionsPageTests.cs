using Bunit;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using FSH.Dashboard.Wasm.Pages.SystemPages;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.System;

public sealed class SessionsPageTests : TestSetup
{
    private readonly ISessionService _sessionService = Substitute.For<ISessionService>();

    public SessionsPageTests()
    {
        Services.AddSingleton(_sessionService);
    }

    [Fact]
    public void Renders_header_and_search()
    {
        _sessionService.GetTenantSessionsAsync(
                Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<UserSessionDto>([], 1, 25, 0, 0, false, false));

        var cut = Render<SessionsPage>();

        cut.Markup.ShouldContain("Sessions");
        cut.Markup.ShouldContain("Include inactive");
    }

    [Fact]
    public void Shows_empty_message_when_no_sessions()
    {
        _sessionService.GetTenantSessionsAsync(
                Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<UserSessionDto>([], 1, 25, 0, 0, false, false));

        var cut = Render<SessionsPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No sessions found"));
    }

    [Fact]
    public void Shows_session_data_when_present()
    {
        var sessions = new List<UserSessionDto>
        {
            new(
                Id: Guid.NewGuid(),
                UserId: Guid.NewGuid().ToString(),
                UserName: "Alice",
                UserEmail: "alice@acme.com",
                IpAddress: "192.168.1.1",
                DeviceType: "Desktop",
                Browser: "Chrome",
                BrowserVersion: "120",
                OperatingSystem: "Windows",
                OsVersion: "11",
                CreatedAt: DateTime.UtcNow.AddDays(-1),
                LastActivityAt: DateTime.UtcNow.AddMinutes(-5),
                ExpiresAt: DateTime.UtcNow.AddHours(1),
                IsActive: true,
                IsCurrentSession: false),
        };

        _sessionService.GetTenantSessionsAsync(
                Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<UserSessionDto>(sessions, 1, 25, 1, 1, false, false));

        var cut = Render<SessionsPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Alice"));
        cut.Markup.ShouldContain("alice@acme.com");
        cut.Markup.ShouldContain("Chrome");
    }

    [Fact]
    public void Shows_You_badge_for_current_session()
    {
        var sessions = new List<UserSessionDto>
        {
            new(
                Id: Guid.NewGuid(),
                UserId: Guid.NewGuid().ToString(),
                UserName: "Current",
                UserEmail: "current@acme.com",
                IpAddress: "10.0.0.1",
                DeviceType: "Desktop",
                Browser: "Firefox",
                BrowserVersion: "121",
                OperatingSystem: "Linux",
                OsVersion: "6.5",
                CreatedAt: DateTime.UtcNow.AddHours(-2),
                LastActivityAt: DateTime.UtcNow.AddMinutes(-1),
                ExpiresAt: DateTime.UtcNow.AddHours(1),
                IsActive: true,
                IsCurrentSession: true),
        };

        _sessionService.GetTenantSessionsAsync(
                Arg.Any<string?>(), Arg.Any<bool?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<UserSessionDto>(sessions, 1, 25, 1, 1, false, false));

        var cut = Render<SessionsPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("You"));
    }
}
