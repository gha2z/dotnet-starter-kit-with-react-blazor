using FSH.Admin.Wasm.Pages.Impersonation;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Models.Tenants;
using FSH.BlazorShared.Services;
using FSH.BlazorShared.Components;
using FSH.BlazorShared.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Tenants;

public sealed partial class TenantDetailPage : IAsyncDisposable
{
    [Parameter] public string Id { get; set; } = string.Empty;

    [Inject] private ITenantService TenantService { get; set; } = default!;
    [Inject] private IImpersonationService ImpersonationService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IRuntimeConfigService Config { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private TenantStatusDto? _tenant;
    private TenantProvisioningStatusDto? _provisioning;
    private string? _error;
    private bool _isActivating;
    private bool _isRetrying;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private CancellationTokenSource? _pollCts;
    private Task? _pollTask;

    private string HeaderTitle => _tenant?.Name ?? "Tenant";

    private Color ProvisioningChipColor => _provisioning?.Status switch
    {
        "Completed" => Color.Success,
        "Failed" => Color.Error,
        "Running" => Color.Info,
        _ => Color.Default
    };

    private string ProvisioningSummary => _provisioning?.Status switch
    {
        "Failed" => $"Failed at {_provisioning!.CurrentStep ?? "unknown step"}",
        "Running" when !string.IsNullOrEmpty(_provisioning!.CurrentStep) => $"Running · {_provisioning!.CurrentStep}",
        _ => _provisioning?.Status ?? "Unknown"
    };

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var statusTask = TenantService.GetStatusAsync(Id);
            var provisioningTask = TenantService.GetProvisioningAsync(Id);
            await Task.WhenAll(statusTask, provisioningTask);
            _tenant = statusTask.Result;
            _provisioning = provisioningTask.Result;
            StartPollingIfRunning();
        }
        catch (Exception ex)
        {
            _error = $"Failed to load tenant: {ex.Message}";
        }
    }

    private void StartPollingIfRunning()
    {
        if (_provisioning is null ||
            _provisioning.Status == "Completed" ||
            _provisioning.Status == "Failed" ||
            _provisioning.Status == "NotTracked")
        {
            return;
        }

        _pollCts = new CancellationTokenSource();
        _pollTask = PollLoopAsync(_pollCts.Token);
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(PollInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                _provisioning = await TenantService.GetProvisioningAsync(Id);
                StateHasChanged();
                if (_provisioning is null ||
                    _provisioning.Status == "Completed" ||
                    _provisioning.Status == "Failed")
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Disposed — stop polling quietly.
        }
        catch (Exception)
        {
            // Transient polling error — keep the last known state; stop to avoid a hot loop.
        }
    }

    private async Task OpenImpersonateDialogAsync()
    {
        var parameters = new DialogParameters<ImpersonateDialog>
        {
            { x => x.TargetTenantId, _tenant!.Id },
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<ImpersonateDialog>("Impersonate user", parameters, options);
        var result = await dialog.Result;
        if (result is null || result.Canceled || result.Data is not ImpersonationResponse response)
        {
            return;
        }

        var url = BuildHandoffUrl(response, _tenant!.Id);
        await Js.InvokeVoidAsync("openUrl", url);
        Snackbar.Add("Opened the dashboard as the impersonated user. End impersonation from inside the dashboard tab.", Severity.Info);
    }

    private string BuildHandoffUrl(ImpersonationResponse response, string tenantId)
    {
        var query = $"token={Uri.EscapeDataString(response.AccessToken)}" +
                    $"&tenant={Uri.EscapeDataString(tenantId)}" +
                    $"&expiresAt={Uri.EscapeDataString(response.AccessTokenExpiresAt.ToString("o"))}";
        return $"{Config.DashboardUrl.TrimEnd('/')}/#impersonate?{query}";
    }

    private async Task OpenRenewDialogAsync()
    {
        var parameters = new DialogParameters<RenewTenantDialog>
        {
            { x => x.TenantId, _tenant!.Id },
            { x => x.CurrentPlanKey, _tenant!.Plan },
            { x => x.ValidUpto, _tenant!.ValidUpto },
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<RenewTenantDialog>("Renew / change plan", parameters, options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled && result.Data is RenewTenantResponse)
        {
            await ReloadAsync();
        }
    }

    private async Task OpenAdjustDialogAsync()
    {
        var parameters = new DialogParameters<AdjustTenantValidityDialog>
        {
            { x => x.TenantId, _tenant!.Id },
            { x => x.ValidUpto, _tenant!.ValidUpto },
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<AdjustTenantValidityDialog>("Adjust validity", parameters, options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled && result.Data is AdjustTenantValidityResponse)
        {
            await ReloadAsync();
        }
    }

    private async Task OpenActivationConfirmAsync()
    {
        var activating = !_tenant!.IsActive;
        var title = activating ? "Activate tenant?" : "Deactivate tenant?";
        var message = activating
            ? $"{_tenant.Name}'s users will be able to sign in and use the platform again."
            : $"Users of {_tenant.Name} will be blocked from signing in and all their API requests will be rejected until you reactivate the tenant.";
        var parameters = new DialogParameters
        {
            { "Message", message },
            { "ConfirmText", activating ? "Activate" : "Deactivate" },
            { "CancelText", "Cancel" },
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };
        var dialog = await DialogService.ShowAsync<FshConfirmDialogContent>(title, parameters, options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled)
        {
            await ChangeActivationAsync(activating);
        }
    }

    private async Task ChangeActivationAsync(bool isActive)
    {
        _isActivating = true;
        try
        {
            var result = await TenantService.ChangeActivationAsync(
                new ChangeTenantActivationRequest(_tenant!.Id, isActive));
            Snackbar.Add(result.Message, Severity.Success);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Activation change failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isActivating = false;
        }
    }

    private async Task RetryProvisioningAsync()
    {
        _isRetrying = true;
        try
        {
            _provisioning = await TenantService.RetryProvisioningAsync(_tenant!.Id);
            Snackbar.Add("Provisioning re-queued", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Retry failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isRetrying = false;
        }
    }

    private async Task ReloadAsync()
    {
        try
        {
            _tenant = await TenantService.GetStatusAsync(Id);
            _provisioning = await TenantService.GetProvisioningAsync(Id);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to reload tenant: {ex.Message}", Severity.Error);
        }
    }

    private void GoBackAsync() => Nav.NavigateTo("/tenants");

    private string Initials
    {
        get
        {
            var letters = _tenant?.Name.Where(char.IsLetterOrDigit).Take(2).ToArray() ?? [];
            return letters.Length > 0 ? new string(letters).ToUpperInvariant() : "?";
        }
    }

    private static Color StepChipColor(string status) => status switch
    {
        "Completed" => Color.Success,
        "Failed" => Color.Error,
        "Running" => Color.Info,
        _ => Color.Default
    };

    private static string FormatDate(DateTime value) => value.ToString("MMM d, yyyy");

    private static string FormatDuration(DateTime start, DateTime end)
    {
        var elapsed = end - start;
        if (elapsed < TimeSpan.FromSeconds(1))
        {
            return $"{elapsed.TotalMilliseconds:0}ms";
        }

        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return $"{elapsed.TotalSeconds:0.0}s";
        }

        return $"{(int)elapsed.TotalMinutes}m {elapsed.Seconds}s";
    }

    public async ValueTask DisposeAsync()
    {
        if (_pollCts is not null)
        {
            await _pollCts.CancelAsync();
            _pollCts.Dispose();
        }

        if (_pollTask is not null)
        {
            try
            {
                await _pollTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected — polling was cancelled.
            }
        }
    }
}
