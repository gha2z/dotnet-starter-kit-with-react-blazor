using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Bunit;
using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Auth;

public sealed class ImpersonationBannerTests : TestSetup
{
    private readonly IImpersonationService _impersonationService = Substitute.For<IImpersonationService>();

    public ImpersonationBannerTests()
    {
        Services.AddSingleton(_impersonationService);
    }

    private sealed class FixedAuthStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        private readonly Task<AuthenticationState> _state = Task.FromResult(new AuthenticationState(user));

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => _state;
    }

    private static ClaimsPrincipal Principal(
        string subject = "su1",
        string tenant = "acme",
        string name = "Jane Doe",
        string? actor = null,
        string? actorTenant = null,
        string? actorName = null)
    {
        var claims = new List<Claim>
        {
            new("sub", subject),
            new("tenant", tenant),
            new("name", name),
        };
        if (actor is not null) claims.Add(new Claim("act_sub", actor));
        if (actorTenant is not null) claims.Add(new Claim("act_tenant", actorTenant));
        if (actorName is not null) claims.Add(new Claim("act_name", actorName));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static string MakeToken(string tenant)
    {
        var jwt = new JwtSecurityToken(
            claims: [new Claim("tenant", tenant)],
            expires: DateTime.UtcNow.AddMinutes(5));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private void Authorize(ClaimsPrincipal principal)
        => Services.AddSingleton<AuthenticationStateProvider>(new FixedAuthStateProvider(principal));

    private IRenderedComponent<FSH.Dashboard.Wasm.Auth.ImpersonationBanner> RenderBanner()
        => Render<FSH.Dashboard.Wasm.Auth.ImpersonationBanner>();

    [Fact]
    public void Renders_nothing_for_a_regular_sign_in()
    {
        Authorize(Principal());

        var cut = RenderBanner();

        cut.FindComponents<MudAlert>().ShouldBeEmpty();
    }

    [Fact]
    public void Shows_subject_and_actor_with_warning_tone_for_same_tenant()
    {
        Authorize(Principal(actor: "op1", actorTenant: "acme", actorName: "Opera Admin"));

        var cut = RenderBanner();

        cut.Markup.ShouldContain("Impersonating Jane Doe");
        cut.Markup.ShouldContain("by Opera Admin (acme)");
        cut.FindComponent<MudAlert>().Instance.Severity.ShouldBe(Severity.Warning);
    }

    [Fact]
    public void Uses_error_tone_for_cross_tenant_impersonation()
    {
        Authorize(Principal(actor: "root", actorTenant: "root", actorName: "Root Admin"));

        var cut = RenderBanner();

        cut.FindComponent<MudAlert>().Instance.Severity.ShouldBe(Severity.Error);
    }

    [Fact]
    public async Task End_without_stash_ends_grant_best_effort_and_signs_out()
    {
        Authorize(Principal(actor: "op1", actorTenant: "acme", actorName: "Opera Admin"));
        TokenStore.HasImpersonationStashAsync().Returns(false);

        var cut = RenderBanner();
        await cut.Find("button").ClickAsync();
        await Task.Delay(150);

        await _impersonationService.Received(1).EndImpersonationAsync(Arg.Any<CancellationToken>());
        await TokenStore.Received(1).ClearAsync();
        Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/login");
    }

    [Fact]
    public async Task End_with_stash_installs_fresh_operator_tokens()
    {
        Authorize(Principal(actor: "op1", actorTenant: "acme", actorName: "Opera Admin"));
        TokenStore.HasImpersonationStashAsync().Returns(true);
        var fresh = new TokenResponse(MakeToken("acme"), "rt1", null, null);
        _impersonationService.EndImpersonationAsync(Arg.Any<CancellationToken>()).Returns(fresh);

        var cut = RenderBanner();
        await cut.Find("button").ClickAsync();

        await TokenStore.Received(1).SetFreshTokensAsync(fresh.AccessToken, fresh.RefreshToken);
        await TokenStore.DidNotReceive().ClearAsync();
        await PermissionsProvider.Received(1).InvalidateCache();
        Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/");
    }

    [Fact]
    public async Task End_with_stash_from_root_operator_signs_out_instead_of_installing_tenant_tokens()
    {
        Authorize(Principal(actor: "root", actorTenant: "root", actorName: "Root Admin"));
        TokenStore.HasImpersonationStashAsync().Returns(true);
        _impersonationService.EndImpersonationAsync(Arg.Any<CancellationToken>())
            .Returns(new TokenResponse(MakeToken("root"), "rt1", null, null));

        var cut = RenderBanner();
        await cut.Find("button").ClickAsync();

        await TokenStore.DidNotReceiveWithAnyArgs().SetFreshTokensAsync(default!, default!);
        await TokenStore.Received(1).ClearAsync();
        Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/login");
    }

    [Fact]
    public async Task End_failure_restores_the_stashed_operator_session()
    {
        Authorize(Principal(actor: "op1", actorTenant: "acme", actorName: "Opera Admin"));
        TokenStore.HasImpersonationStashAsync().Returns(true);
        _impersonationService.EndImpersonationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TokenResponse>(new InvalidOperationException("grant already ended")));

        var cut = RenderBanner();
        await cut.Find("button").ClickAsync();

        await TokenStore.Received(1).RestoreTokensAsync();
        await PermissionsProvider.Received(1).InvalidateCache();
        Services.GetRequiredService<NavigationManager>().Uri.ShouldBe("http://localhost/");
    }
}
