using Bunit;
using FSH.Admin.Wasm.Pages.Identity.Roles;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Identity.Roles;

public class RoleCreateDialogTests : TestSetup
{
    private readonly IRoleService _roleService = Substitute.For<IRoleService>();

    public RoleCreateDialogTests()
    {
        Services.AddSingleton(_roleService);
    }

    private async Task<(IRenderedComponent<MudDialogProvider> Provider, IDialogReference Reference)> ShowDialogAsync()
    {
        var provider = Render<MudDialogProvider>();
        var dialogService = Services.GetRequiredService<IDialogService>();
        var reference = await dialogService.ShowAsync<RoleCreateDialog>("New role");
        provider.WaitForAssertion(() => provider.FindAll("input").Count.ShouldBe(2));
        return (provider, reference);
    }

    [Fact]
    public async Task Empty_form_shows_validation_errors_and_does_not_create()
    {
        var (provider, reference) = await ShowDialogAsync();

        provider.FindAll("button").First(b => b.TextContent.Contains("Create role")).Click();

        provider.WaitForAssertion(() => provider.Markup.ShouldContain("Name is required."));
        await _roleService.DidNotReceive().UpsertAsync(Arg.Any<UpsertRoleRequest>(), Arg.Any<CancellationToken>());
        reference.Result.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task Valid_form_creates_role_and_closes_dialog()
    {
        _roleService.UpsertAsync(Arg.Any<UpsertRoleRequest>(), Arg.Any<CancellationToken>())
            .Returns(new RoleDto("11111111-1111-1111-1111-111111111111", "Support", "Inbound support", null));
        var (provider, reference) = await ShowDialogAsync();

        var inputs = provider.FindAll("input");
        inputs[0].Change("Support agent");
        inputs[1].Change("Inbound support");

        provider.FindAll("button").First(b => b.TextContent.Contains("Create role")).Click();

        await _roleService.Received(1).UpsertAsync(
            Arg.Is<UpsertRoleRequest>(r =>
                r.Id == string.Empty
                && r.Name == "Support agent"
                && r.Description == "Inbound support"),
            Arg.Any<CancellationToken>());

        var result = await reference.Result;
        result!.Canceled.ShouldBeFalse();
        result.Data.ShouldBeOfType<RoleDto>().Name.ShouldBe("Support");
    }

    [Fact]
    public async Task Cancel_closes_dialog_without_calling_service()
    {
        var (provider, reference) = await ShowDialogAsync();

        provider.FindAll("button").First(b => b.TextContent.Contains("Cancel")).Click();

        var result = await reference.Result;
        result!.Canceled.ShouldBeTrue();
        await _roleService.DidNotReceive().UpsertAsync(Arg.Any<UpsertRoleRequest>(), Arg.Any<CancellationToken>());
    }
}
