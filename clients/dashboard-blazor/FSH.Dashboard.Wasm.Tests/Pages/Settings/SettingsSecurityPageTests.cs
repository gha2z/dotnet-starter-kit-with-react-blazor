using Bunit;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using FSH.Dashboard.Wasm.Pages.Settings;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Settings;

public sealed class SettingsSecurityPageTests : TestSetup
{
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly ISessionService _sessionService = Substitute.For<ISessionService>();

    public SettingsSecurityPageTests()
    {
        Services.AddSingleton(_userService);
        Services.AddSingleton(_sessionService);
    }

    [Fact]
    public void Renders_header_and_sessions_when_present()
    {
        var sessions = new List<UserSessionDto>
        {
            new(
                Guid.NewGuid(), "user-1", "jdoe", "jane@acme.com", "10.0.0.1", "desktop",
                "Chrome", "122", "Windows", "11", DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow.AddHours(1),
                true, true),
            new(
                Guid.NewGuid(), "user-1", "jdoe", "jane@acme.com", "10.0.0.2", "mobile",
                "Safari", "17", "iOS", "17", DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow.AddHours(1),
                true, false),
        };
        _sessionService.GetMySessionsAsync(Arg.Any<CancellationToken>()).Returns(sessions);

        var cut = Render<SettingsSecurityPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Active sessions"));
        cut.Markup.ShouldContain("This device");
        cut.Markup.ShouldContain("10.0.0.2");
    }

    [Fact]
    public void Shows_empty_state_when_no_sessions()
    {
        _sessionService.GetMySessionsAsync(Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render<SettingsSecurityPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No active sessions tracked."));
    }

    [Fact]
    public void Has_change_password_button_and_opening_uses_dialog_service()
    {
        _sessionService.GetMySessionsAsync(Arg.Any<CancellationToken>()).Returns([]);
        var dialogService = Services.GetRequiredService<IDialogService>();

        var cut = Render<SettingsSecurityPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Change password"));

        cut.FindAll("button").First(b => b.TextContent.Contains("Change password")).Click();

        // DialogService was invoked to show the ChangePasswordDialog component.
        cut.WaitForAssertion(() =>
            cut.Markup.ShouldContain("Change password"));
    }
}
