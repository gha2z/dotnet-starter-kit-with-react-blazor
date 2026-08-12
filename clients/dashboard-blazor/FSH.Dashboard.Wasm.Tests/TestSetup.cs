using Bunit;
using Bunit.TestDoubles;
using FSH.BlazorShared.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Services;
using NSubstitute;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests;

public abstract class TestSetup : BunitContext, IAsyncLifetime
{
    protected readonly BunitAuthorizationContext Authorization;
    protected readonly ITokenStore TokenStore;
    protected readonly IPermissionsProvider PermissionsProvider;

    protected TestSetup()
    {
        DefaultWaitTimeout = TimeSpan.FromSeconds(30);
        JSInterop.Mode = JSRuntimeMode.Loose;
        TokenStore = Substitute.For<ITokenStore>();
        PermissionsProvider = Substitute.For<IPermissionsProvider>();

        Services.AddSingleton(TokenStore);
        Services.AddScoped<AuthStateProvider>();
        Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<AuthStateProvider>());
        Services.AddScoped(_ => PermissionsProvider);
        Services.AddMudServices();
        Authorization = AddAuthorization();
    }

    /// <summary>
    /// Minimal IJSObjectReference standing in for the fshTheme.js module —
    /// stores/returns theme values the same way localStorage does.
    /// </summary>
    protected sealed class FakeJsObject : IJSObjectReference
    {
        public bool PrefersDark { get; set; }
        private readonly Dictionary<string, string?> _stored = new(StringComparer.Ordinal);

        public void Store(string key, string? value) => _stored[key] = value;

        public string? GetStored(string key)
            => _stored.TryGetValue(key, out var value) ? value : null;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "getTheme" && args?.Length == 1 && args[0] is string key)
            {
                return ValueTask.FromResult(GetStored(key) is { } stored ? (TValue)(object)stored : default(TValue)!)!;
            }

            if (identifier == "prefersDark")
            {
                return ValueTask.FromResult((TValue)(object)PrefersDark)!;
            }

            if (identifier == "setTheme" && args?.Length == 2 && args[0] is string key2 && args[1] is string value)
            {
                _stored[key2] = value;
            }

            if (identifier == "getPreference" && args?.Length == 1 && args[0] is string prefKey)
            {
                return ValueTask.FromResult(GetStored(prefKey) is { } stored ? (TValue)(object)stored : default(TValue)!)!;
            }

            if (identifier == "setPreference" && args?.Length == 2 && args[0] is string prefKey2 && args[1] is string value2)
            {
                _stored[prefKey2] = value2;
            }

            return ValueTask.FromResult(default(TValue))!;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() =>
        ((IAsyncDisposable)this).DisposeAsync().AsTask();
}
