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

public sealed class ChangePasswordDialogTests : TestSetup
{
    private readonly IUserService _userService = Substitute.For<IUserService>();

    public ChangePasswordDialogTests()
    {
        Services.AddSingleton(_userService);
    }

    private (IRenderedComponent<MudDialogProvider> Provider, IRenderedComponent<ChangePasswordDialog> Dialog) ShowDialog()
    {
        var provider = Render<MudDialogProvider>();
        var dialogService = Services.GetRequiredService<IDialogService>();
        _ = dialogService.ShowAsync<ChangePasswordDialog>("Change password");
        provider.WaitForAssertion(() => provider.FindAll("input").Count.ShouldBe(3));
        var dialog = provider.FindComponent<ChangePasswordDialog>();
        return (provider, dialog);
    }

    [Fact]
    public void Renders_all_three_password_fields()
    {
        var (_, dialog) = ShowDialog();

        dialog.Markup.ShouldContain("Current password");
        dialog.Markup.ShouldContain("New password");
        dialog.Markup.ShouldContain("Confirm new password");
    }

    [Fact]
    public void Validates_mismatched_confirm_without_calling_service()
    {
        var (_, dialog) = ShowDialog();

        var inputs = dialog.FindAll("input");
        inputs[0].Input("old-pass");
        inputs = dialog.FindAll("input");
        inputs[1].Input("new-pass-123");
        inputs = dialog.FindAll("input");
        inputs[2].Input("different-pass");

        dialog.FindAll("button").First(b => b.TextContent.Contains("Update password")).Click();

        dialog.WaitForAssertion(() => dialog.Markup.ShouldContain("Passwords don't match"));
        _userService.DidNotReceive().ChangePasswordAsync(Arg.Any<ChangePasswordRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validates_short_new_password_without_calling_service()
    {
        var (_, dialog) = ShowDialog();

        var inputs = dialog.FindAll("input");
        inputs[0].Input("old-pass");
        inputs = dialog.FindAll("input");
        inputs[1].Input("short");
        inputs = dialog.FindAll("input");
        inputs[2].Input("short");

        dialog.FindAll("button").First(b => b.TextContent.Contains("Update password")).Click();

        dialog.WaitForAssertion(() => dialog.Markup.ShouldContain("at least 8 characters"));
        _userService.DidNotReceive().ChangePasswordAsync(Arg.Any<ChangePasswordRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Calls_service_and_closes_dialog_when_valid()
    {
        var (_, dialog) = ShowDialog();

        var inputs = dialog.FindAll("input");
        inputs[0].Input("old-pass");
        inputs = dialog.FindAll("input");
        inputs[1].Input("new-pass-123");
        inputs = dialog.FindAll("input");
        inputs[2].Input("new-pass-123");

        dialog.FindAll("button").First(b => b.TextContent.Contains("Update password")).Click();

        dialog.WaitForAssertion(() =>
            _userService.Received(1).ChangePasswordAsync(
                Arg.Is<ChangePasswordRequest>(r => r.NewPassword == "new-pass-123"),
                Arg.Any<CancellationToken>()));
    }
}
