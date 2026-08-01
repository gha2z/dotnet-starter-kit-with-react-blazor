using Bunit;
using FSH.Admin.Wasm.Pages.Tenants;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Models.Tenants;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Tenants;

public class TenantCreateDialogTests : TestSetup
{
    private readonly ITenantService _tenantService = Substitute.For<ITenantService>();
    private readonly IBillingService _billingService = Substitute.For<IBillingService>();

    public TenantCreateDialogTests()
    {
        Services.AddSingleton(_tenantService);
        Services.AddSingleton(_billingService);
        _billingService.GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private async Task<(IRenderedComponent<MudDialogProvider> Provider, IDialogReference Reference)> ShowDialogAsync()
    {
        var provider = Render<MudDialogProvider>();
        var dialogService = Services.GetRequiredService<IDialogService>();
        var reference = await dialogService.ShowAsync<TenantCreateDialog>("New tenant");
        provider.WaitForAssertion(() => provider.Markup.ShouldContain("Display name"));
        return (provider, reference);
    }

    private static void FillValidForm(IRenderedComponent<MudDialogProvider> provider)
    {
        provider.Find("input#ct-name").Change("Acme Corp");
        provider.WaitForAssertion(() => provider.Find("input#ct-id").GetAttribute("value").ShouldBe("acme-corp"));
        provider.Find("input#ct-adminEmail").Change("admin@acme.example");
        provider.Find("input#ct-adminPassword").Change("P@ssw0rd!123");
    }

    [Fact]
    public async Task Empty_form_shows_validation_errors_and_does_not_create()
    {
        var (provider, reference) = await ShowDialogAsync();

        provider.FindAll("button").First(b => b.TextContent.Contains("Create tenant")).Click();

        provider.WaitForAssertion(() => provider.Markup.ShouldContain("Display name is required."));
        provider.Markup.ShouldContain("Identifier is required.");
        provider.Markup.ShouldContain("Admin email is required.");
        provider.Markup.ShouldContain("Admin password is required.");
        await _tenantService.DidNotReceive().CreateAsync(Arg.Any<CreateTenantRequest>(), Arg.Any<CancellationToken>());
        reference.Result.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task Valid_form_creates_tenant_and_closes_dialog()
    {
        _tenantService.CreateAsync(Arg.Any<CreateTenantRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateTenantResponse("acme-corp", "corr-1", "Queued"));
        var (provider, reference) = await ShowDialogAsync();

        FillValidForm(provider);
        provider.Find("input#ct-issuer").Change("custom-issuer");

        // The submit is async and the click can race the renderer replacing the
        // button's event handler. Re-find the button on every attempt (bUnit's
        // documented workaround for UnknownEventHandlerIdException) and poll
        // until the request lands; a disabled button is never re-clicked.
        provider.WaitForAssertion(() =>
        {
            provider.FindAll("button")
                .FirstOrDefault(b => b.TextContent.Contains("Create tenant") && !b.HasAttribute("disabled"))
                ?.Click();
            _tenantService.Received(1).CreateAsync(
                Arg.Is<CreateTenantRequest>(r =>
                    r.Id == "acme-corp"
                    && r.Name == "Acme Corp"
                    && r.AdminEmail == "admin@acme.example"
                    && r.AdminPassword == "P@ssw0rd!123"
                    && r.Issuer == "custom-issuer"
                    && r.ConnectionString == null
                    && r.PlanKey == null),
                Arg.Any<CancellationToken>());
        });

        var result = await reference.Result;
        result!.Canceled.ShouldBeFalse();
        result.Data.ShouldBeOfType<CreateTenantResponse>().Id.ShouldBe("acme-corp");
    }

    [Fact]
    public async Task Typing_name_auto_derives_identifier_and_issuer()
    {
        var (provider, _) = await ShowDialogAsync();

        provider.Find("input#ct-name").Change("Acme Corp");

        provider.WaitForAssertion(() =>
        {
            provider.Find("input#ct-id").GetAttribute("value").ShouldBe("acme-corp");
            provider.Find("input#ct-issuer").GetAttribute("value").ShouldBe("acme-corp");
        });
    }

    [Fact]
    public async Task Identifier_unlock_stops_auto_slugging()
    {
        var (provider, _) = await ShowDialogAsync();

        provider.Find("input#ct-name").Change("Acme Corp");
        provider.WaitForAssertion(() => provider.Find("input#ct-id").GetAttribute("value").ShouldBe("acme-corp"));

        provider.Find("button.mud-input-adornment-icon-button").Click();
        provider.Find("input#ct-id").Change("my-tenant");
        provider.Find("input#ct-name").Change("Acme Corp Ltd");

        provider.WaitForAssertion(() =>
            provider.Find("input#ct-id").GetAttribute("value").ShouldBe("my-tenant"));
    }

    [Fact]
    public async Task Cancel_closes_dialog_without_calling_service()
    {
        var (provider, reference) = await ShowDialogAsync();

        provider.FindAll("button").First(b => b.TextContent.Contains("Cancel")).Click();

        var result = await reference.Result;
        result!.Canceled.ShouldBeTrue();
        await _tenantService.DidNotReceive().CreateAsync(Arg.Any<CreateTenantRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Plan_is_preselected_and_sent_with_request()
    {
        _billingService.GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(
            [
                new BillingPlanDto(Guid.NewGuid(), "free", "Free", "USD", 0m, true, "Monthly", null),
                new BillingPlanDto(Guid.NewGuid(), "pro", "Pro", "USD", 49m, true, "Monthly", 490m),
            ]);
        _tenantService.CreateAsync(Arg.Any<CreateTenantRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateTenantResponse("acme-corp", "corr-1", "Queued"));
        var (provider, reference) = await ShowDialogAsync();

        provider.WaitForAssertion(() => provider.Markup.ShouldContain("Free · Monthly · USD 0.00"));
        FillValidForm(provider);

        // Same retry-click pattern as the valid-form test: re-find the button
        // on every attempt and poll until the request lands.
        provider.WaitForAssertion(() =>
        {
            provider.FindAll("button")
                .FirstOrDefault(b => b.TextContent.Contains("Create tenant") && !b.HasAttribute("disabled"))
                ?.Click();
            _tenantService.Received(1).CreateAsync(
                Arg.Is<CreateTenantRequest>(r => r.PlanKey == "free"),
                Arg.Any<CancellationToken>());
        });

        var result = await reference.Result;
        result!.Canceled.ShouldBeFalse();
    }
}
