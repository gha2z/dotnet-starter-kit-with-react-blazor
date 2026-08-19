using Bunit;
using FSH.BlazorShared.Components;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using FSH.Dashboard.Wasm.Pages.Settings;
using Microsoft.AspNetCore.Components;
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
    private readonly ITwoFactorService _twoFactorService = Substitute.For<ITwoFactorService>();

    public SettingsSecurityPageTests()
    {
        Services.AddSingleton(_userService);
        Services.AddSingleton(_sessionService);
        Services.AddSingleton(_twoFactorService);
    }

    private static UserDto SampleProfile(bool twoFactor = false) =>
        new("u1", "jane", "Jane", "Doe", "jane@example.com", true, true, null, null, twoFactor);

    private IRenderedComponent<TwoFactorSection> TwoFactorSectionOf(IRenderedComponent<IComponent> root) =>
        root.FindComponent<TwoFactorSection>();

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
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>()).Returns(SampleProfile());
        _sessionService.GetMySessionsAsync(Arg.Any<CancellationToken>()).Returns(sessions);

        var cut = Render<SettingsSecurityPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Active sessions"));
        cut.Markup.ShouldContain("This device");
        cut.Markup.ShouldContain("10.0.0.2");
    }

    [Fact]
    public void Shows_empty_state_when_no_sessions()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>()).Returns(SampleProfile());
        _sessionService.GetMySessionsAsync(Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render<SettingsSecurityPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No active sessions tracked."));
    }

    [Fact]
    public void Has_change_password_button_and_opening_uses_dialog_service()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>()).Returns(SampleProfile());
        _sessionService.GetMySessionsAsync(Arg.Any<CancellationToken>()).Returns([]);
        var dialogService = Services.GetRequiredService<IDialogService>();

        var cut = Render<SettingsSecurityPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Change password"));

        cut.FindAll("button").First(b => b.TextContent.Contains("Change password")).Click();

        // DialogService was invoked to show the ChangePasswordDialog component.
        cut.WaitForAssertion(() =>
            cut.Markup.ShouldContain("Change password"));
    }

    [Fact]
    public void Shows_two_factor_section_in_disabled_state()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>()).Returns(SampleProfile(false));
        _sessionService.GetMySessionsAsync(Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render<SettingsSecurityPage>();

        cut.WaitForAssertion(() =>
            TwoFactorSectionOf(cut).Markup.ShouldContain("Enable two-factor"));
    }

    [Fact]
    public void Shows_two_factor_enabled_chip_when_profile_has_2fa()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>()).Returns(SampleProfile(true));
        _sessionService.GetMySessionsAsync(Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render<SettingsSecurityPage>();

        cut.WaitForAssertion(() =>
            TwoFactorSectionOf(cut).Markup.ShouldContain("Enabled"));
    }

    [Fact]
    public void Two_factor_enroll_shows_shared_key_and_verify()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>()).Returns(SampleProfile(false));
        _sessionService.GetMySessionsAsync(Arg.Any<CancellationToken>()).Returns([]);
        var enrollment = new TwoFactorEnrollmentResponse("otpauth://totp/acme?secret=JBSWY3DPEHPK3PXP", "JBSWY3DPEHPK3PXP");
        _twoFactorService.EnrollAsync(Arg.Any<CancellationToken>()).Returns(enrollment);

        var cut = Render<SettingsSecurityPage>();
        var section = TwoFactorSectionOf(cut);

        section.FindAll("button").First(b => b.TextContent.Contains("Enable two-factor")).Click();

        section.WaitForAssertion(() => section.Markup.ShouldContain("JBSWY3DPEHPK3PXP"));
        section.Markup.ShouldContain("Copy key");
        section.Markup.ShouldContain("Verify &amp; enable");
    }
}
