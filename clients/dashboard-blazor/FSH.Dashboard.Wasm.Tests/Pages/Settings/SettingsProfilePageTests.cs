using Bunit;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using FSH.Dashboard.Wasm.Pages.Settings;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Settings;

public sealed class SettingsProfilePageTests : TestSetup
{
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IFileService _fileService = Substitute.For<IFileService>();

    public SettingsProfilePageTests()
    {
        Services.AddSingleton(_userService);
        Services.AddSingleton(_fileService);
    }

    [Fact]
    public void Renders_header_and_loaded_profile_values()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>())
            .Returns(new UserDto("user-1", "jdoe", "Jane", "Doe", "jane@acme.com", true, true, "+15550123", null, false));

        var cut = Render<SettingsProfilePage>();

        cut.WaitForAssertion(() => cut.FindAll("input").Count.ShouldBeGreaterThan(0));
        cut.Markup.ShouldContain("jane@acme.com");
        cut.Markup.ShouldContain("user-1");
    }

    [Fact]
    public void Shows_error_band_when_profile_load_fails()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<UserDto>(new InvalidOperationException("boom")));

        var cut = Render<SettingsProfilePage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to load profile"));
    }

    [Fact]
    public void Save_dispatches_update_after_editing_fields()
    {
        var profile = new UserDto("user-1", "jdoe", "Jane", "Doe", "jane@acme.com", true, true, "+15550123", null, false);
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>())
            .Returns(profile);
        _userService.UpdateMyProfileAsync(Arg.Any<UpdateProfileRequest>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        var cut = Render<SettingsProfilePage>();
        cut.WaitForAssertion(() => cut.FindAll("input").Count.ShouldBe(5));

        // The first text input is the file picker from the image input; the first
        // non-file input is the "First name" field.
        cut.FindAll("input").First(i => i.GetAttribute("type") != "file").Input("Janet");
        cut.WaitForAssertion(() => cut.FindAll("button").Any(b => b.TextContent.Contains("Save changes")));
        cut.FindAll("button").First(b => b.TextContent.Contains("Save changes")).Click();

        cut.WaitForAssertion(() =>
            _userService.Received(1).UpdateMyProfileAsync(
                Arg.Is<UpdateProfileRequest>(r => r.FirstName == "Janet"),
                Arg.Any<CancellationToken>()));
    }

    [Fact]
    public void Image_picker_label_is_wired_to_the_hidden_file_input()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>())
            .Returns(new UserDto("user-1", "jdoe", "Jane", "Doe", "jane@acme.com", true, true, "+15550123", null, false));

        var cut = Render<SettingsProfilePage>();
        cut.WaitForAssertion(() => cut.FindAll("input").Count.ShouldBeGreaterThan(0));

        var fileInput = cut.Find("input[type=file]");
        var picker = cut.FindAll("label.fsh-image-picker").ShouldHaveSingleItem();

        picker.GetAttribute("for").ShouldBe(fileInput.Id);
        picker.TextContent.ShouldContain("Choose image");
        fileInput.ClassList.ShouldContain("fsh-input-file-hidden");
    }

    [Fact]
    public void Image_change_raises_profile_updated_event()
    {
        _userService.GetMyProfileAsync(Arg.Any<CancellationToken>())
            .Returns(new UserDto("user-1", "jdoe", "Jane", "Doe", "jane@acme.com", true, true, "+15550123", null, false));

        var raised = 0;
        ProfileEvents.ProfileUpdated += Handler;
        try
        {
            var cut = Render<SettingsProfilePage>();
            cut.WaitForAssertion(() => cut.FindAll("input").Count.ShouldBeGreaterThan(0));

            // Switch to Paste URL mode, then type an image URL into the text field.
            cut.FindAll("button.fsh-image-mode").First(b => b.TextContent.Contains("Paste URL")).Click();
            var urlField = cut.FindAll("input").First(i => i.GetAttribute("type") != "file");
            urlField.Change("https://example.com/avatar.png");

            cut.WaitForAssertion(() =>
                _userService.Received(1).SetProfileImageAsync("https://example.com/avatar.png", Arg.Any<CancellationToken>()));
            raised.ShouldBe(1);
        }
        finally
        {
            ProfileEvents.ProfileUpdated -= Handler;
        }

        void Handler() => raised++;
    }
}
