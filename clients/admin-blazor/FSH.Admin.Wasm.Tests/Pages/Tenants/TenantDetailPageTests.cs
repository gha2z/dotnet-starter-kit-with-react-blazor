using Bunit;
using FSH.Admin.Wasm.Pages.Tenants;
using FSH.BlazorShared.Models.Tenants;
using FSH.BlazorShared.Permissions;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Tenants;

public class TenantDetailPageTests : TestSetup
{
    private const string TenantId = "acme-corp";

    private readonly ITenantService _tenantService = Substitute.For<ITenantService>();
    private readonly IImpersonationService _impersonationService = Substitute.For<IImpersonationService>();
    private readonly ITenantThemeService _themeService = Substitute.For<ITenantThemeService>();

    public TenantDetailPageTests()
    {
        Services.AddSingleton(_tenantService);
        Services.AddSingleton(_impersonationService);
        Services.AddSingleton(_themeService);
    }

    private static TenantStatusDto SampleStatus(bool isActive = true, string? plan = "pro", string expiryState = "Active") =>
        new()
        {
            Id = TenantId,
            Name = "Acme Corp",
            IsActive = isActive,
            ValidUpto = new DateTime(2027, 1, 15),
            AdminEmail = "admin@acme.example",
            Issuer = "acme-corp",
            Plan = plan,
            ExpiryState = expiryState,
            HasConnectionString = false,
        };

    private static TenantProvisioningStatusDto SampleProvisioning(string status, params TenantProvisioningStepDto[] steps) =>
        new(
            TenantId,
            status,
            "corr-1",
            status == "Running" ? "SeedAdmin" : null,
            status == "Failed" ? "seed failed" : null,
            DateTime.UtcNow.AddMinutes(-2),
            DateTime.UtcNow.AddMinutes(-2),
            status == "Running" ? null : DateTime.UtcNow,
            steps);

    private void StubStatus(TenantStatusDto status) =>
        _tenantService.GetStatusAsync(TenantId, Arg.Any<CancellationToken>()).Returns(status);

    private void StubProvisioning(TenantProvisioningStatusDto? provisioning) =>
        _tenantService.GetProvisioningAsync(TenantId, Arg.Any<CancellationToken>()).Returns(provisioning);

    private IRenderedComponent<TenantDetailPage> RenderPage() =>
        Render<TenantDetailPage>(p => p.Add(p => p.Id, TenantId));

    [Fact]
    public void Renders_overview_and_details_with_not_tracked_provisioning()
    {
        StubStatus(SampleStatus());
        StubProvisioning(null);

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Acme Corp"));
        cut.Markup.ShouldContain("admin@acme.example");
        cut.Markup.ShouldContain("acme-corp");
        cut.Markup.ShouldContain("pro");
        cut.Markup.ShouldContain("Valid until Jan 15, 2027");
        cut.Markup.ShouldContain("iss · acme-corp");
        cut.Markup.ShouldContain("Shared catalog");
        cut.Markup.ShouldContain("no run history to show");
    }

    [Fact]
    public void Expired_tenant_shows_grace_badge_and_plan()
    {
        StubStatus(SampleStatus(plan: "free", expiryState: "InGrace"));
        StubProvisioning(null);

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("In grace"));
        cut.Markup.ShouldContain("free");
    }

    [Fact]
    public void Renders_provisioning_step_timeline()
    {
        StubStatus(SampleStatus());
        StubProvisioning(SampleProvisioning(
            "Completed",
            new TenantProvisioningStepDto("CreateDatabase", "Completed", DateTime.UtcNow.AddMinutes(-2), DateTime.UtcNow.AddMinutes(-1), null),
            new TenantProvisioningStepDto("SeedAdmin", "Completed", DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, null)));

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("CreateDatabase"));
        cut.Markup.ShouldContain("SeedAdmin");
    }

    [Fact]
    public void Failed_provisioning_shows_error_and_retry_calls_service()
    {
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(MultitenancyPermissions.Tenants.Update);
        StubStatus(SampleStatus());
        StubProvisioning(SampleProvisioning("Failed"));
        _tenantService.RetryProvisioningAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(SampleProvisioning("Completed"));

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed at unknown step"));
        cut.Markup.ShouldContain("seed failed");

        cut.FindAll("button").First(b => b.TextContent.Contains("Retry provisioning")).Click();

        _tenantService.Received(1).RetryProvisioningAsync(TenantId, Arg.Any<CancellationToken>());
        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("Failed at unknown step"));
    }

    [Fact]
    public void Deactivate_flow_confirms_then_calls_service()
    {
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(MultitenancyPermissions.Tenants.Update);
        StubStatus(SampleStatus());
        StubProvisioning(null);
        _tenantService.ChangeActivationAsync(Arg.Any<ChangeTenantActivationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TenantLifecycleResultDto { TenantId = TenantId, IsActive = false, Message = "Tenant deactivated." });

        var cut = Render(builder =>
        {
            builder.OpenComponent<MudDialogProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<TenantDetailPage>(1);
            builder.AddComponentParameter(1, "Id", TenantId);
            builder.CloseComponent();
        });

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Deactivate tenant"));
        cut.FindAll("button").First(b => b.TextContent.Contains("Deactivate tenant")).Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Deactivate tenant?"));
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Deactivate").Click();

        _tenantService.Received(1).ChangeActivationAsync(
            Arg.Is<ChangeTenantActivationRequest>(r => r.TenantId == TenantId && !r.IsActive),
            Arg.Any<CancellationToken>());
        cut.WaitForAssertion(() => _tenantService.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(ITenantService.GetStatusAsync)).ShouldBe(2));
    }

    [Fact]
    public void Action_buttons_hidden_without_permissions()
    {
        StubStatus(SampleStatus());
        StubProvisioning(null);

        var cut = RenderPage();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Acme Corp"));

        cut.FindAll("button").Any(b => b.TextContent.Contains("Deactivate tenant")).ShouldBeFalse();
        cut.FindAll("button").Any(b => b.TextContent.Contains("Renew / change plan")).ShouldBeFalse();
        cut.FindAll("button").Any(b => b.TextContent.Contains("Adjust validity")).ShouldBeFalse();
    }

    [Fact]
    public void Load_failure_shows_alert()
    {
        _tenantService.GetStatusAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TenantStatusDto>(new InvalidOperationException("nope")));
        _tenantService.GetProvisioningAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TenantProvisioningStatusDto?>(new InvalidOperationException("nope")));

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to load tenant: nope"));
    }

    [Fact]
    public void Impersonate_button_visible_with_permission_on_active_tenant()
    {
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(IdentityPermissions.Users.Impersonate);
        StubStatus(SampleStatus());
        StubProvisioning(null);

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Impersonate user"));
    }

    [Fact]
    public void Impersonate_button_hidden_without_permission()
    {
        StubStatus(SampleStatus());
        StubProvisioning(null);

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Acme Corp"));
        cut.FindAll("button").Any(b => b.TextContent.Contains("Impersonate user")).ShouldBeFalse();
    }

    [Fact]
    public void Impersonate_button_hidden_on_inactive_tenant()
    {
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(IdentityPermissions.Users.Impersonate);
        StubStatus(SampleStatus(isActive: false));
        StubProvisioning(null);

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Acme Corp"));
        cut.FindAll("button").Any(b => b.TextContent.Contains("Impersonate user")).ShouldBeFalse();
    }

    [Fact]
    public void Branding_card_renders_theme_when_view_theme_permission()
    {
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(MultitenancyPermissions.Tenants.ViewTheme);
        StubStatus(SampleStatus());
        StubProvisioning(null);
        _themeService.GetThemeAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new FSH.BlazorShared.Models.Tenants.TenantThemeDto
            {
                LightPalette = new FSH.BlazorShared.Models.Tenants.PaletteDto { Primary = "#123456" },
            });

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Branding"));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("#123456"));
    }

    [Fact]
    public void Branding_card_hidden_without_view_theme_permission()
    {
        StubStatus(SampleStatus());
        StubProvisioning(null);

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Acme Corp"));
        cut.Markup.ShouldNotContain("Save branding");
    }

    [Fact]
    public void Running_provisioning_polls_until_completed()
    {
        StubStatus(SampleStatus());
        var running = SampleProvisioning("Running");
        var completed = SampleProvisioning("Completed");
        var calls = 0;
        _tenantService.GetProvisioningAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(_ => Interlocked.Increment(ref calls) == 1 ? running : completed);

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Completed"));
        calls.ShouldBeGreaterThanOrEqualTo(2);
    }
}
