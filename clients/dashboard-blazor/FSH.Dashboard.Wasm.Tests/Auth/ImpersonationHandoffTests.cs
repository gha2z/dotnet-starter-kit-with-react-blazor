using FSH.BlazorShared.Auth;
using FSH.Dashboard.Wasm.Auth;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Auth;

public sealed class ImpersonationHandoffTests
{
    private readonly ITokenStore _tokenStore = Substitute.For<ITokenStore>();
    private readonly IPermissionsProvider _permissions = Substitute.For<IPermissionsProvider>();
    private readonly AuthStateProvider _authState;
    private readonly ImpersonationHandoff _handoff;

    public ImpersonationHandoffTests()
    {
        _authState = new AuthStateProvider(_tokenStore, _permissions);
        _handoff = new ImpersonationHandoff(_tokenStore, _authState);
    }

    [Fact]
    public async Task Null_or_unrelated_hash_is_ignored()
    {
        (await _handoff.InstallFromHashAsync(null)).ShouldBeFalse();
        (await _handoff.InstallFromHashAsync("#some-route")).ShouldBeFalse();
        (await _handoff.InstallFromHashAsync("#impersonation:legacy-format")).ShouldBeFalse();

        await _tokenStore.DidNotReceiveWithAnyArgs().SetTokensAsync(default!, default!);
        await _tokenStore.DidNotReceiveWithAnyArgs().SetTenantAsync(default!);
    }

    [Fact]
    public async Task Malformed_hash_without_token_is_ignored()
    {
        (await _handoff.InstallFromHashAsync("#impersonate?tenant=acme")).ShouldBeFalse();

        await _tokenStore.DidNotReceiveWithAnyArgs().SetTokensAsync(default!, default!);
    }

    [Fact]
    public async Task Valid_hash_installs_impersonation_session_without_refresh()
    {
        _tokenStore.GetAccessTokenAsync().Returns((string?)null);

        var installed = await _handoff.InstallFromHashAsync("#impersonate?token=t1&tenant=acme&expiresAt=2026-08-06T00%3A00%3A00Z");

        installed.ShouldBeTrue();
        await _tokenStore.Received(1).SetTokensAsync("t1", null);
        await _tokenStore.Received(1).SetTenantAsync("acme");
        await _permissions.Received(1).ResetAsync();
        await _tokenStore.DidNotReceive().StashTokensAsync();
    }

    [Fact]
    public async Task Valid_hash_stashes_the_existing_operator_session()
    {
        _tokenStore.GetAccessTokenAsync().Returns("operator-token");

        var installed = await _handoff.InstallFromHashAsync("#impersonate?token=t1&tenant=acme");

        installed.ShouldBeTrue();
        await _tokenStore.Received(1).StashTokensAsync();
        await _tokenStore.Received(1).SetTokensAsync("t1", null);
    }

    [Fact]
    public async Task Url_encoded_values_are_decoded()
    {
        await _handoff.InstallFromHashAsync("#impersonate?token=eyJhbGciOi%2BIUzI1NiJ9&tenant=my%20tenant");

        await _tokenStore.Received(1).SetTokensAsync("eyJhbGciOi+IUzI1NiJ9", null);
        await _tokenStore.Received(1).SetTenantAsync("my tenant");
    }
}
