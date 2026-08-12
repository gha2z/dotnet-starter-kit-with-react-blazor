using Bunit;
using FSH.BlazorShared.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Theming;

/// <summary>
/// Empirical probe for the "theme switch needs a reload" report. Renders a probe
/// component that wires MudThemeProvider exactly the way the real App.razor does
/// (service Changed => new theme instance + StateHasChanged) and asserts the emitted
/// <c>&lt;style&gt;</c> block actually regenerates when the mode flips. bUnit runs
/// the real render pipeline (diffing + MarkupString), so a pass proves the render
/// chain regenerates the CSS in-process; a fail localises the defect to the
/// render/diff layer.
/// </summary>
public sealed class MudThemeProviderReapplyProbeTests : TestSetup
{
    private readonly FshThemeService _themeService;

    public MudThemeProviderReapplyProbeTests()
    {
        _themeService = new InMemoryThemeService(JSInterop.JSRuntime);
        Services.AddSingleton(_themeService);
    }

    [Fact]
    public void Style_Block_Regenerates_When_Mode_Flips()
    {
        var cut = Render<ThemeProbe>();

        var styleBefore = cut.Find("style.mud-theme-provider").TextContent;
        styleBefore.ShouldNotBeNullOrWhiteSpace();

        cut.InvokeAsync(() => _themeService.SetModeAsync(ThemeMode.Dark));

        cut.WaitForAssertion(() =>
        {
            var styleAfter = cut.Find("style.mud-theme-provider").TextContent;
            styleAfter.ShouldNotBe(styleBefore, "dark palette must emit different CSS variables than light");
        });
    }

    /// <summary>Mirror of App.razor's MudThemeProvider wiring.</summary>
    private sealed class ThemeProbe : ComponentBase, IDisposable
    {
        [Inject] private FshThemeService Theme { get; set; } = default!;

        private MudTheme _theme = FshMudTheme.CreateDashboard();

        protected override void OnInitialized()
        {
            Theme.Changed += OnThemeChanged;
        }

        private void OnThemeChanged()
        {
            _theme = FshMudTheme.CreateDashboard(FshAppearanceOptions.ResolveAccent(Theme.AccentId, Theme.CustomAccent));
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            Theme.Changed -= OnThemeChanged;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<MudThemeProvider>(0);
            builder.AddAttribute(1, "Theme", _theme);
            builder.AddAttribute(2, "IsDarkMode", Theme.IsDarkMode);
            builder.CloseComponent();
        }
    }

    /// <summary>
    /// In-memory theme service so the probe never touches the JS module in bUnit's
    /// loose mode — preferences round-trip through a dictionary instead.
    /// </summary>
    private sealed class InMemoryThemeService : FshThemeService
    {
        private readonly Dictionary<string, string?> _stored = new(StringComparer.Ordinal);

        public InMemoryThemeService(IJSRuntime js)
            : base(js, "fsh.theme", ThemeMode.Light)
        {
        }

        protected override Task<string?> ReadPreferenceAsync(string key)
            => Task.FromResult(_stored.TryGetValue(key, out var value) ? value : null);

        protected override Task WritePreferenceAsync(string key, string value)
        {
            _stored[key] = value;
            return Task.CompletedTask;
        }
    }
}