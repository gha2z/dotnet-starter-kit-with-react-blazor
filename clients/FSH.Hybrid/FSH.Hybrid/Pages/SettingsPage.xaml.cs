using FSH.Hybrid.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Hybrid.Pages;

public sealed partial class SettingsPage : ContentPage
{
    private MauiAuthStateProvider? _authState;
    private IBiometricService? _biometric;
    private HybridRuntimeConfigService? _config;

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _authState ??= Application.Current?.Handler?.MauiContext?.Services.GetService<MauiAuthStateProvider>();
        _biometric ??= Application.Current?.Handler?.MauiContext?.Services.GetService<IBiometricService>();
        _config ??= Application.Current?.Handler?.MauiContext?.Services.GetService<HybridRuntimeConfigService>();

        if (_authState is not null)
        {
            BiometricSwitch.IsToggled = _authState.IsBiometricLockEnabled;
        }

        if (_biometric is not null)
        {
            _ = RefreshBiometricStatusAsync();
        }

        if (_config is not null)
        {
            ApiBaseEntry.Text = _config.ApiBaseUrl;
            ApiBaseHintLabel.Text = $"Applied immediately for new HTTP calls (currently: {_config.ApiBaseUrl})";
        }
    }

    private async Task RefreshBiometricStatusAsync()
    {
        if (_biometric is null)
        {
            return;
        }

        var available = await _biometric.IsAvailableAsync();
        BiometricStatusLabel.Text = available
            ? "Available on this device"
            : "Not available on this device";
    }

    private void OnBiometricLockToggled(object? sender, ToggledEventArgs e)
    {
        if (_authState is not null)
        {
            _authState.IsBiometricLockEnabled = e.Value;
        }
    }

    private void OnApiBaseChanged(object? sender, EventArgs e)
    {
        var value = ApiBaseEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (_config is not null && _config.ApiBaseUrl != value)
        {
            _config.ApiBaseUrl = value;
            Preferences.Default.Set("fsh.hybrid.apiBase", value);
        }

        ApiBaseHintLabel.Text = $"Applied immediately for new HTTP calls (currently: {value})";
    }
}
