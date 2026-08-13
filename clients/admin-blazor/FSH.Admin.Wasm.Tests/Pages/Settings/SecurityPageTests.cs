using Bunit;
using FSH.Admin.Wasm.Pages.Settings;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Settings;

public class SecurityPageTests : TestSetup
{
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly ITwoFactorService _twoFactorService = Substitute.For<ITwoFactorService>();

    public SecurityPageTests()
    {
        Services.AddSingleton(_userService);
        Services.AddSingleton(_twoFactorService);
    }

    private static UserDto SampleProfile(bool twoFactor = false) =>
        new("u1", "jane", "Jane", "Doe", "jane@example.com", true, true, null, null, twoFactor);

    private IRenderedComponent<PasswordSection> PasswordSectionOf(IRenderedComponent<IComponent> root) =>
        root.FindComponent<PasswordSection>();

    private IRenderedComponent<TwoFactorSection> TwoFactorSectionOf(IRenderedComponent<IComponent> root) =>
        root.FindComponent<TwoFactorSection>();

    [Fact]
    public void Renders_password_and_2fa_sections()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>()).Returns(SampleProfile());

        var cut = Render<SecurityPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Password");
            cut.Markup.ShouldContain("Two-factor authentication");
            cut.Markup.ShouldContain("Current password");
            cut.Markup.ShouldContain("New password");
        });
    }

    [Fact]
    public void Shows_enable_button_when_2fa_is_off()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>()).Returns(SampleProfile(twoFactor: false));

        var cut = Render<SecurityPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Enable two-factor"));
    }

    [Fact]
    public void Shows_disable_button_when_2fa_is_on()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>()).Returns(SampleProfile(twoFactor: true));

        var cut = Render<SecurityPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Enabled");
            cut.Markup.ShouldContain("Disable");
        });
    }

    [Fact]
    public void Changing_password_validates_required_and_minimum_length()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>()).Returns(SampleProfile());

        var cut = Render<SecurityPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Current password"));

        PasswordSectionOf(cut).FindAll("button").First(b => b.TextContent.Contains("Change password")).Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("At least 8 characters."));
    }

    [Fact]
    public void Changing_password_rejects_same_as_current()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>()).Returns(SampleProfile());

        var cut = Render<SecurityPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Current password"));

        var section = PasswordSectionOf(cut);
        var inputs = section.FindAll("input");
        inputs[0].Change("S3cret!pass");
        inputs[1].Change("S3cret!pass");
        inputs[2].Change("S3cret!pass");
        section.FindAll("button").First(b => b.TextContent.Contains("Change password")).Click();

        cut.WaitForAssertion(() => _userService.DidNotReceiveWithAnyArgs().ChangePasswordAsync(default!, default));
    }

    [Fact]
    public void Changing_password_submits_request_and_confirms()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>()).Returns(SampleProfile());
        _userService.ChangePasswordAsync(Arg.Any<ChangePasswordRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = Render<SecurityPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Current password"));

        var section = PasswordSectionOf(cut);
        var inputs = section.FindAll("input");
        inputs[0].Change("Old!pass");
        inputs[1].Change("New!pass123");
        inputs[2].Change("New!pass123");
        section.FindAll("button").First(b => b.TextContent.Contains("Change password")).Click();

        cut.WaitForAssertion(() =>
        {
            _userService.Received(1)
                .ChangePasswordAsync(
                    new ChangePasswordRequest("Old!pass", "New!pass123", "New!pass123"),
                    Arg.Any<CancellationToken>());
            cut.Markup.ShouldContain("Password updated.");
        });
    }

    [Fact]
    public void Enrolling_shows_shared_key_and_verify()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>()).Returns(SampleProfile());
        _twoFactorService.EnrollAsync(Arg.Any<CancellationToken>())
            .Returns(new TwoFactorEnrollmentResponse("JBSWY3DPEHPK3PXP", "otpauth://totp/acme"));

        var cut = Render<SecurityPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Enable two-factor"));

        TwoFactorSectionOf(cut).FindAll("button").First(b => b.TextContent.Contains("Enable two-factor")).Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("JBSWY3DPEHPK3PXP");
            cut.Markup.ShouldContain("Copy key");
            cut.Markup.ShouldContain("Verify &amp; enable");
        });
    }

    [Fact]
    public void Verifying_invalid_code_shows_error()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>()).Returns(SampleProfile());
        _twoFactorService.EnrollAsync(Arg.Any<CancellationToken>())
            .Returns(new TwoFactorEnrollmentResponse("JBSWY3DPEHPK3PXP", "otpauth://totp/acme"));
        _twoFactorService.VerifyEnrollAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var cut = Render<SecurityPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Enable two-factor"));
        var section = TwoFactorSectionOf(cut);
        section.FindAll("button").First(b => b.TextContent.Contains("Enable two-factor")).Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Verify &amp; enable"));

        section.FindAll("input")[^1].Change("000000");
        section.FindAll("button").First(b => b.TextContent.Contains("Verify")).Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("That code wasn't accepted"));
    }

    [Fact]
    public void Verifying_valid_code_fires_on_changed()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>()).Returns(SampleProfile());
        _twoFactorService.EnrollAsync(Arg.Any<CancellationToken>())
            .Returns(new TwoFactorEnrollmentResponse("JBSWY3DPEHPK3PXP", "otpauth://totp/acme"));
        _twoFactorService.VerifyEnrollAsync("123456", Arg.Any<CancellationToken>()).Returns(true);

        var cut = Render<SecurityPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Enable two-factor"));
        var section = TwoFactorSectionOf(cut);
        section.FindAll("button").First(b => b.TextContent.Contains("Enable two-factor")).Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Verify &amp; enable"));

        section.FindAll("input")[^1].Change("123456");
        section.FindAll("button").First(b => b.TextContent.Contains("Verify")).Click();

        cut.WaitForAssertion(() =>
            _twoFactorService.Received(1).VerifyEnrollAsync("123456", Arg.Any<CancellationToken>()));
    }

    [Fact]
    public void Disabling_2fa_requires_password_and_confirms()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>()).Returns(SampleProfile(twoFactor: true));
        _twoFactorService.DisableAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var cut = Render<TestShell>(p => p.AddChildContent<SecurityPage>());
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Disable"));
        cut.Markup.ShouldNotContain("Disable 2FA");

        var section = TwoFactorSectionOf(cut);
        section.FindAll("button").First(b => b.TextContent.Contains("Disable")).Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Enter your password to confirm"));

        cut.FindAll("div.mud-dialog input").Single().Change("My!Passw0rd");
        cut.FindAll("button").First(b => b.TextContent.Contains("Disable 2FA")).Click();

        cut.WaitForAssertion(() =>
            _twoFactorService.Received(1).DisableAsync("My!Passw0rd", Arg.Any<CancellationToken>()));
    }

    [Fact]
    public void Shows_error_band_when_profile_load_fails()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<UserDto>(new Exception("security boom")));

        var cut = Render<SecurityPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("security boom"));
    }
}
