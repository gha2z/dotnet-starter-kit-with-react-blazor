using Bunit;
using AngleSharp.Dom;
using FSH.Admin.Wasm.Pages.Identity.Users;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Identity.Users;

public class UserCreateDialogTests : TestSetup
{
    private readonly IUserService _userService = Substitute.For<IUserService>();

    public UserCreateDialogTests()
    {
        Services.AddSingleton(_userService);
    }

    private async Task<(IRenderedComponent<MudDialogProvider> Provider, IDialogReference Reference)> ShowDialogAsync()
    {
        var provider = Render<MudDialogProvider>();
        var dialogService = Services.GetRequiredService<IDialogService>();
        var reference = await dialogService.ShowAsync<UserCreateDialog>("New user");
        provider.WaitForAssertion(() => provider.FindAll("input").Count.ShouldBe(7));
        return (provider, reference);
    }

    private static void FillValidForm(IReadOnlyList<IElement> inputs)
    {
        inputs[0].Change("Jane");
        inputs[1].Change("Doe");
        inputs[2].Change("janedoe");
        inputs[3].Change("jane@example.com");
        inputs[4].Change("+1-555-0100");
        inputs[5].Change("Str0ng!Pass");
        inputs[6].Change("Str0ng!Pass");
    }

    [Fact]
    public async Task Empty_form_shows_validation_errors_and_does_not_create()
    {
        var (provider, reference) = await ShowDialogAsync();

        provider.FindAll("button").First(b => b.TextContent.Contains("Create account")).Click();

        provider.WaitForAssertion(() => provider.Markup.ShouldContain("First name is required."));
        provider.Markup.ShouldContain("Last name is required.");
        provider.Markup.ShouldContain("Username is required.");
        provider.Markup.ShouldContain("Email is required.");
        provider.Markup.ShouldContain("Password is required.");
        provider.Markup.ShouldContain("Confirm your password.");
        await _userService.DidNotReceive().CreateAsync(Arg.Any<RegisterUserRequest>(), Arg.Any<CancellationToken>());
        reference.Result.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task Valid_form_creates_user_and_closes_dialog()
    {
        _userService.CreateAsync(Arg.Any<RegisterUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(new RegisterUserResponse("u1", "User Created"));
        var (provider, reference) = await ShowDialogAsync();

        FillValidForm(provider.FindAll("input"));

        provider.FindAll("button").First(b => b.TextContent.Contains("Create account")).Click();

        await _userService.Received(1).CreateAsync(
            Arg.Is<RegisterUserRequest>(r =>
                r.FirstName == "Jane"
                && r.LastName == "Doe"
                && r.UserName == "janedoe"
                && r.Email == "jane@example.com"
                && r.Password == "Str0ng!Pass"
                && r.ConfirmPassword == "Str0ng!Pass"
                && r.PhoneNumber == "+1-555-0100"),
            Arg.Any<CancellationToken>());

        var result = await reference.Result;
        result!.Canceled.ShouldBeFalse();
        result.Data.ShouldBeOfType<RegisterUserResponse>().UserId.ShouldBe("u1");
    }

    [Fact]
    public async Task Password_mismatch_blocks_create()
    {
        var (provider, reference) = await ShowDialogAsync();

        var inputs = provider.FindAll("input");
        inputs[0].Change("Jane");
        inputs[1].Change("Doe");
        inputs[2].Change("janedoe");
        inputs[3].Change("jane@example.com");
        inputs[5].Change("Str0ng!Pass");
        inputs[6].Change("Different!");

        provider.FindAll("button").First(b => b.TextContent.Contains("Create account")).Click();

        provider.WaitForAssertion(() => provider.Markup.ShouldContain("Passwords do not match."));
        await _userService.DidNotReceive().CreateAsync(Arg.Any<RegisterUserRequest>(), Arg.Any<CancellationToken>());
        reference.Result.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task Service_failure_keeps_dialog_open()
    {
        _userService.CreateAsync(Arg.Any<RegisterUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<RegisterUserResponse>(new InvalidOperationException("gone")));
        var (provider, reference) = await ShowDialogAsync();

        FillValidForm(provider.FindAll("input"));

        provider.FindAll("button").First(b => b.TextContent.Contains("Create account")).Click();

        await Task.Delay(100);
        reference.Result.IsCompleted.ShouldBeFalse();
        provider.Markup.ShouldContain("Create account");
    }
}
