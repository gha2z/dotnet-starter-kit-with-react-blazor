using System.Net.Http;
using Bunit;
using Bunit.TestDoubles;
using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Infrastructure;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using NSubstitute;
using Xunit;

namespace FSH.Admin.Wasm.Tests;

public abstract class TestSetup : BunitContext, IAsyncLifetime
{
    protected readonly ITokenStore TokenStore;
    protected readonly BunitAuthorizationContext Authorization;
    protected readonly IPermissionsProvider PermissionsProvider;

    protected TestSetup()
    {
        DefaultWaitTimeout = TimeSpan.FromSeconds(30);
        TokenStore = Substitute.For<ITokenStore>();
        PermissionsProvider = Substitute.For<IPermissionsProvider>();

        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton(TokenStore);
        Services.AddScoped<AuthStateProvider>();
        Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<AuthStateProvider>());
        Services.AddScoped(_ => PermissionsProvider);
        Authorization = AddAuthorization();
        Services.AddMudServices();

        // Register services needed for impersonation page
        Services.AddSingleton<IRuntimeConfigService, RuntimeConfigServiceStub>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() =>
        ((IAsyncDisposable)this).DisposeAsync().AsTask();
}
