using FSH.BlazorShared.Theming;
using Microsoft.JSInterop;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Theming;

public sealed class FshThemeServiceTests
{
    private const string ThemeModulePath = "./_content/FSH.BlazorShared/js/fshTheme.js";

    /// <summary>Fake IJSRuntime that returns the fake module for "import" and lets
    /// the module store/read theme values the way localStorage does.</summary>
    private sealed class FakeJSRuntime(FakeJsObject jsObject) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "import" && args?.Length == 1 && args[0] is string path && path == ThemeModulePath)
            {
                return ValueTask.FromResult((TValue)(object)jsObject)!;
            }

            return ValueTask.FromResult(default(TValue))!;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);
    }

    private sealed class FakeJsObject : IJSObjectReference
    {
        public bool PrefersDark { get; set; }
        private readonly Dictionary<string, string?> _stored = new(StringComparer.Ordinal);

        public void Store(string key, string? value) => _stored[key] = value;
        public string? GetStored(string key) => _stored.TryGetValue(key, out var value) ? value : null;

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

            return ValueTask.FromResult(default(TValue))!;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private static FshThemeService CreateService(FakeJsObject jsObject, string storageKey = "fsh.theme", ThemeMode defaultMode = ThemeMode.System)
        => new(new FakeJSRuntime(jsObject), storageKey, defaultMode);

    [Fact]
    public async Task InitializeAsync_When_Nothing_Stored_Defaults_To_System_And_Resolves_Os()
    {
        var service = CreateService(new FakeJsObject());

        await service.InitializeAsync();

        service.Mode.ShouldBe(ThemeMode.System);
        service.IsDarkMode.ShouldBeFalse();
    }

    [Fact]
    public async Task InitializeAsync_When_Os_Prefers_Dark_Resolves_System_To_Dark()
    {
        var service = CreateService(new FakeJsObject { PrefersDark = true });

        await service.InitializeAsync();

        service.Mode.ShouldBe(ThemeMode.System);
        service.IsDarkMode.ShouldBeTrue();
    }

    [Fact]
    public async Task InitializeAsync_When_Stored_Dark_Applies_Dark()
    {
        var jsObject = new FakeJsObject();
        jsObject.Store("fsh.theme", "dark");
        var service = CreateService(jsObject);

        await service.InitializeAsync();

        service.Mode.ShouldBe(ThemeMode.Dark);
        service.IsDarkMode.ShouldBeTrue();
    }

    [Fact]
    public async Task SetModeAsync_Persists_Light_And_Raises_Changed()
    {
        var jsObject = new FakeJsObject();
        var service = CreateService(jsObject);
        await service.InitializeAsync();

        var changed = 0;
        service.Changed += () => changed++;
        await service.SetModeAsync(ThemeMode.Light);

        service.Mode.ShouldBe(ThemeMode.Light);
        service.IsDarkMode.ShouldBeFalse();
        changed.ShouldBe(1);
        jsObject.GetStored("fsh.theme").ShouldBe("light");
    }

    [Fact]
    public async Task SetModeAsync_System_Resolves_Os_Preference_Immediately()
    {
        var jsObject = new FakeJsObject();
        var service = CreateService(jsObject);
        await service.InitializeAsync();
        await service.SetModeAsync(ThemeMode.Light);

        jsObject.PrefersDark = true;
        await service.SetModeAsync(ThemeMode.System);

        service.Mode.ShouldBe(ThemeMode.System);
        service.IsDarkMode.ShouldBeTrue();
        jsObject.GetStored("fsh.theme").ShouldBe("system");
    }

    [Fact]
    public async Task SetAsync_Behaves_Like_Binary_Toggle_And_Persists_Dark()
    {
        var jsObject = new FakeJsObject();
        var service = CreateService(jsObject);
        await service.InitializeAsync();

        await service.SetAsync(true);

        service.Mode.ShouldBe(ThemeMode.Dark);
        service.IsDarkMode.ShouldBeTrue();
        jsObject.GetStored("fsh.theme").ShouldBe("dark");
    }

    [Fact]
    public async Task SetModeAsync_Same_Mode_Is_No_Op()
    {
        var service = CreateService(new FakeJsObject { PrefersDark = false });
        await service.InitializeAsync();

        var changed = 0;
        service.Changed += () => changed++;
        await service.SetModeAsync(ThemeMode.System);

        changed.ShouldBe(0);
        service.Mode.ShouldBe(ThemeMode.System);
    }

    [Fact]
    public async Task Admin_Binary_Default_Is_Dark()
    {
        var service = CreateService(new FakeJsObject(), "fsh.admin.theme", ThemeMode.Dark);

        await service.InitializeAsync();

        service.Mode.ShouldBe(ThemeMode.Dark);
        service.IsDarkMode.ShouldBeTrue();
    }
}
