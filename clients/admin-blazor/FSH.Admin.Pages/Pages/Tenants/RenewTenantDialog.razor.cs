using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Models.Tenants;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Tenants;

public sealed partial class RenewTenantDialog
{
    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = default!;
    [Inject] private ITenantService TenantService { get; set; } = default!;
    [Inject] private IBillingService BillingService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Parameter] public string TenantId { get; set; } = string.Empty;
    [Parameter] public string? CurrentPlanKey { get; set; }
    [Parameter] public DateTime ValidUpto { get; set; }

    private List<BillingPlanDto> _plans = [];
    private string _selectedPlanKey = string.Empty;
    private bool _isSubmitting;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _plans = await BillingService.GetPlansAsync(includeInactive: false);
        }
        catch (Exception)
        {
            _plans = [];
        }

        _selectedPlanKey = _plans.FirstOrDefault(p => p.Key == CurrentPlanKey)?.Key
            ?? _plans.FirstOrDefault()?.Key
            ?? string.Empty;
    }

    private async Task SaveAsync()
    {
        _isSubmitting = true;
        try
        {
            var request = new RenewTenantRequest(
                TenantId,
                string.IsNullOrWhiteSpace(_selectedPlanKey) ? null : _selectedPlanKey);
            var result = await TenantService.RenewAsync(request);
            Snackbar.Add(
                result.PlanChanged ? "Plan changed and tenant renewed." : "Tenant renewed.",
                Severity.Success);
            Dialog.Close(result);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Renew failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void Cancel() => Dialog.Cancel();
}
