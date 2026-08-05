using FSH.BlazorShared.Infrastructure;
using FSH.Hybrid.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Hybrid.Pages.Auth;

public sealed partial class LoginPage
{
    private MudForm? _form;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _tenant = string.Empty;
    private bool _showPassword;
    private bool _isSubmitting;
    private bool _isUnlocking;
    private bool _hasSession;
    private bool _biometricAvailable;
    private bool _lockEnabled;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        _lockEnabled = HybridAuth.IsBiometricLockEnabled;
        _hasSession = !string.IsNullOrEmpty(await TokenStore.GetAccessTokenAsync());
        _biometricAvailable = await Biometric.IsAvailableAsync();
    }

    private void TogglePasswordVisibility() => _showPassword = !_showPassword;

    private void OnLockToggleChanged(bool value)
    {
        _lockEnabled = value;
        HybridAuth.IsBiometricLockEnabled = value;
    }

    private async Task SubmitAsync()
    {
        if (_isSubmitting)
        {
            return;
        }

        if (_form is not null)
        {
            await _form.ValidateAsync();
            if (!_form.IsValid)
            {
                return;
            }
        }

        _isSubmitting = true;
        _error = null;
        try
        {
            await Auth.LoginAsync(_email, _password, _tenant);

            var returnUrl = Nav.TryGetQueryString<string>("returnUrl", out var url) && url is not null ? url : "/";
            Nav.NavigateTo(returnUrl);
        }
        catch (HttpRequestException ex)
        {
            _error = ex.StatusCode is not null
                ? $"Login failed ({(int)ex.StatusCode})"
                : "Unable to connect to server. Please try again.";
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task UnlockAsync()
    {
        if (_isUnlocking)
        {
            return;
        }

        _isUnlocking = true;
        _error = null;
        try
        {
            var unlocked = await Auth.TryBiometricUnlockAsync("Unlock FSH Hybrid");
            if (unlocked)
            {
                Nav.NavigateTo("/");
            }
            else
            {
                _error = "Biometric verification failed. Sign in with your password instead.";
            }
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _isUnlocking = false;
        }
    }
}
