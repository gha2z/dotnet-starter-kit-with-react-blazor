using AngleSharp.Dom;
using Bunit;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Identity;

public sealed class UserCreateDialogTests : TestSetup
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
        var reference = await dialogService.ShowAsync<FSH.Dashboard.Wasm.Pages.Identity.UserCreateDialog>("New user");
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

        provider.FindAll("button").First(b => b.TextContent.Contains("Register user", StringComparison.OrdinalIgnoreCase))
            .Click();

        provider.WaitForAssertion(() =>
        {
            provider.Markup.ShouldContain("First name is required.");
            provider.Markup.ShouldContain("Username is required.");
        });
        await _userService.DidNotReceive().CreateAsync(Arg.Any<RegisterUserRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Valid_form_creates_user_and_closes_dialog()
    {
        _userService.CreateAsync(Arg.Any<RegisterUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(new RegisterUserResponse("u1", "User created"));

        var (provider, reference) = await ShowDialogAsync();
        var inputs = provider.FindAll("input");
        FillValidForm(inputs);

        provider.FindAll("button").First(b => b.TextContent.Contains("Register user", StringComparison.OrdinalIgnoreCase))
            .Click();

        await _userService.Received(1).CreateAsync(
            Arg.Is<RegisterUserRequest>(r =>
                r.FirstName == "Jane"
                && r.LastName == "Doe"
                && r.UserName == "janedoe"
                && r.Email == "jane@example.com"),
            Arg.Any<CancellationToken>());
    }
}

