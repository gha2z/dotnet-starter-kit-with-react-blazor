using Bunit;
using FSH.BlazorShared.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using NSubstitute;

namespace FSH.Admin.Wasm.Tests;

public abstract class TestSetup : BunitContext
{
    protected readonly ITokenStore TokenStore;

    protected TestSetup()
    {
        TokenStore = Substitute.For<ITokenStore>();

        Services.AddSingleton(TokenStore);
        Services.AddScoped<AuthStateProvider>();
        Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<AuthStateProvider>());
        Services.AddAuthorizationCore();
        Services.AddMudServices();
    }
}
