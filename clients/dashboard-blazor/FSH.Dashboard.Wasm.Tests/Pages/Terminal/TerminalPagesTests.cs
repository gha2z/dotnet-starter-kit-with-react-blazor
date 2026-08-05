using Bunit;
using FSH.BlazorShared.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Terminal;

public sealed class TerminalPagesTests : TestSetup
{
    [Fact]
    public void Tenant_deactivated_page_renders_copy()
    {
        var cut = Render<FSH.Dashboard.Wasm.Pages.Terminal.TenantDeactivatedPage>();

        cut.Markup.ShouldContain("Tenant deactivated");
        cut.Markup.ShouldContain("contact your administrator");
        cut.FindAll("button").Count.ShouldBe(1);
    }

    [Fact]
    public async Task Tenant_deactivated_back_to_sign_in_clears_session_and_routes_to_login()
    {
        var cut = Render<FSH.Dashboard.Wasm.Pages.Terminal.TenantDeactivatedPage>();

        await cut.Find("button").ClickAsync();

        await TokenStore.Received(1).ClearAsync();
        Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/login");
    }

    [Fact]
    public void Impersonation_ended_page_renders_copy()
    {
        var cut = Render<FSH.Dashboard.Wasm.Pages.Terminal.ImpersonationEndedPage>();

        cut.Markup.ShouldContain("Impersonation ended");
        cut.Markup.ShouldContain("Sign in with your own account to continue");
        cut.FindAll("button").Count.ShouldBe(1);
    }

    [Fact]
    public async Task Impersonation_ended_back_to_sign_in_clears_session_and_routes_to_login()
    {
        var cut = Render<FSH.Dashboard.Wasm.Pages.Terminal.ImpersonationEndedPage>();

        await cut.Find("button").ClickAsync();

        await TokenStore.Received(1).ClearAsync();
        Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/login");
    }
}
