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

public class ProfilePageTests : TestSetup
{
    private readonly IUserService _userService = Substitute.For<IUserService>();

    public ProfilePageTests()
    {
        Services.AddSingleton(_userService);
    }

    private static UserDto SampleProfile(
        bool twoFactor = false,
        string? imageUrl = null,
        bool isActive = true) =>
        new("u1", "jane", "Jane", "Doe", "jane@example.com", isActive, true, "+1 555 0100", imageUrl, twoFactor);

    [Fact]
    public void Renders_identity_fields_from_profile()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>()).Returns(SampleProfile());

        var cut = Render<ProfilePage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Jane Doe");
            cut.Markup.ShouldContain("jane@example.com");
            cut.Markup.ShouldContain("Address verified");
            cut.Markup.ShouldContain("+1 555 0100");
        });
    }

    [Fact]
    public void Shows_active_and_2fa_status_chips()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>())
            .Returns(SampleProfile(twoFactor: true));

        var cut = Render<ProfilePage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Active");
            cut.Markup.ShouldContain("2FA enabled");
        });
    }

    [Fact]
    public void Shows_inactive_status_when_profile_is_disabled()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>())
            .Returns(SampleProfile(isActive: false));

        var cut = Render<ProfilePage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Inactive"));
    }

    [Fact]
    public void Renders_monogram_when_no_avatar()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>())
            .Returns(SampleProfile());

        var cut = Render<ProfilePage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Change avatar");
            cut.Markup.ShouldNotContain("class=\"fsh-avatar-lg\"");
        });
    }

    [Fact]
    public void Renders_avatar_image_and_clear_button_when_image_url_set()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>())
            .Returns(SampleProfile(imageUrl: "https://cdn.example.com/avatar.jpg"));

        var cut = Render<ProfilePage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("fsh-avatar-lg");
            cut.Markup.ShouldContain("Clear");
        });
    }

    [Fact]
    public void Saving_avatar_calls_set_image_and_reloads()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>())
            .Returns(SampleProfile());

        var cut = Render<TestShell>(p => p.AddChildContent<ProfilePage>());
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Change avatar"));

        cut.FindAll("button").First(b => b.TextContent.Contains("Change avatar")).Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Image URL"));

        cut.FindAll("div.mud-dialog input").Single().Change("https://cdn.example.com/new.jpg");
        cut.FindAll("button").First(b => b.TextContent.Contains("Save")).Click();

        cut.WaitForAssertion(() =>
            _userService.Received(1).SetProfileImageAsync("https://cdn.example.com/new.jpg", Arg.Any<CancellationToken>()));
    }

    [Fact]
    public void Shows_error_band_when_profile_load_fails()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<UserDto>(new Exception("profile boom")));

        var cut = Render<ProfilePage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("profile boom"));
    }
}
