using System.Text.Json;
using FSH.BlazorShared.Models.Tenants;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Tenants;

public sealed partial class TenantBrandingCard
{
    [Parameter] public string TenantId { get; set; } = string.Empty;

    [Inject] private ITenantThemeService ThemeService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private TenantThemeDto? _theme;
    private TenantThemeDto? _snapshot;
    private string? _error;
    private bool _isSaving;
    private bool _isResetting;

    private bool IsDirty => _theme is not null && _snapshot is not null &&
        JsonSerializer.Serialize(_theme) != JsonSerializer.Serialize(_snapshot);

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _error = null;
        try
        {
            _theme = await ThemeService.GetThemeAsync(TenantId);
            _snapshot = Clone(_theme);
        }
        catch (Exception ex)
        {
            _error = $"Failed to load branding: {ex.Message}";
        }
    }

    private void OnPaletteChanged() => StateHasChanged();

    private void SetAsset(string value, bool setLogo = false, bool setLogoDark = false, bool setFavicon = false)
    {
        if (setLogo)
        {
            _theme!.BrandAssets.LogoUrl = string.IsNullOrWhiteSpace(value) ? null : value;
            _theme.BrandAssets.DeleteLogo = string.IsNullOrWhiteSpace(value);
        }
        else if (setLogoDark)
        {
            _theme!.BrandAssets.LogoDarkUrl = string.IsNullOrWhiteSpace(value) ? null : value;
            _theme.BrandAssets.DeleteLogoDark = string.IsNullOrWhiteSpace(value);
        }
        else if (setFavicon)
        {
            _theme!.BrandAssets.FaviconUrl = string.IsNullOrWhiteSpace(value) ? null : value;
            _theme.BrandAssets.DeleteFavicon = string.IsNullOrWhiteSpace(value);
        }

        StateHasChanged();
    }

    private async Task SaveAsync()
    {
        _isSaving = true;
        try
        {
            await ThemeService.UpdateThemeAsync(TenantId, _theme!);
            Snackbar.Add("Branding saved", Severity.Success);
            _snapshot = Clone(_theme!);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Save failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task ResetToDefaultsAsync()
    {
        _isResetting = true;
        try
        {
            await ThemeService.ResetThemeAsync(TenantId);
            Snackbar.Add("Branding reset to defaults", Severity.Success);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Reset failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isResetting = false;
        }
    }

    private static TenantThemeDto Clone(TenantThemeDto source) =>
        JsonSerializer.Deserialize<TenantThemeDto>(JsonSerializer.Serialize(source))!;
}
