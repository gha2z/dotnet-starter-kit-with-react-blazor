using Bunit;
using FSH.BlazorShared.Theming;
using FSH.Dashboard.Wasm.Pages.Settings;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Settings;

public sealed class CustomAccentDialogTests : TestSetup
{
    private (IRenderedComponent<MudDialogProvider> Provider, IRenderedComponent<CustomAccentDialog> Dialog) ShowDialog()
    {
        var provider = Render<MudDialogProvider>();
        var dialogService = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters<CustomAccentDialog>
        {
            { x => x.InitialSpec, new FshCustomAccentSpec(220, 1.1) }
        };
        _ = dialogService.ShowAsync<CustomAccentDialog>("Custom accent", parameters);
        provider.WaitForAssertion(() => provider.FindAll(".mud-slider").Count.ShouldBe(2));
        var dialog = provider.FindComponent<CustomAccentDialog>();
        return (provider, dialog);
    }

    [Fact]
    public void Renders_hue_and_intensity_sliders_with_initial_values()
    {
        var (_, dialog) = ShowDialog();

        dialog.Markup.ShouldContain("Hue");
        dialog.Markup.ShouldContain("Intensity");
        dialog.Markup.ShouldContain("220");
        dialog.Markup.ShouldContain("1.1");
    }

    [Fact]
    public void Apply_returns_updated_spec()
    {
        var (provider, dialog) = ShowDialog();

        dialog.FindAll("button").First(b => b.TextContent.Contains("Apply")).Click();

        // Closing the dialog removes it from the render tree.
        provider.WaitForAssertion(() => provider.FindComponents<CustomAccentDialog>().Count.ShouldBe(0));
    }

    [Fact]
    public void Cancel_closes_dialog()
    {
        var (provider, dialog) = ShowDialog();

        dialog.FindAll("button").First(b => b.TextContent.Contains("Cancel")).Click();

        provider.WaitForAssertion(() => provider.FindComponents<CustomAccentDialog>().Count.ShouldBe(0));
    }
}
