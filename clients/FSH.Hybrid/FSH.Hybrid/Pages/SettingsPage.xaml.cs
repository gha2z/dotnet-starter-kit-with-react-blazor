using FSH.BlazorShared.Infrastructure;
using FSH.Hybrid.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Hybrid.Pages;

public sealed partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private MauiAuthStateProvider? _authState;
    private IBiometricService? _biometric;
    private HybridRuntimeConfigService? _config;
    private ILogger<SettingsPage>? _logger;

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // A settings page must never take down the app: resolve lazily and log failures.
        try
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            if (services is null)
            {
                return;
            }

            _logger ??= services.GetService<ILogger<SettingsPage>>();
            _authState ??= services.GetService<MauiAuthStateProvider>();
            _biometric ??= services.GetService<IBiometricService>();
            _config ??= services.GetService<IRuntimeConfigService>() as HybridRuntimeConfigService;

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
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SettingsPage failed to initialize");
        }
    }

    private async Task RefreshBiometricStatusAsync()
    {
        try
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
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Biometric availability check failed");
        }
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
