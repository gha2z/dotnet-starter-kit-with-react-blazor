using FSH.BlazorShared.Models.Tenants;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Tenants;

public sealed partial class AdjustTenantValidityDialog
{
    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = default!;
    [Inject] private ITenantService TenantService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Parameter] public string TenantId { get; set; } = string.Empty;
    [Parameter] public DateTime ValidUpto { get; set; }

    private DateTime? _validUpto;
    private bool _isSubmitting;

    protected override void OnInitialized()
    {
        _validUpto = ValidUpto;
    }

    private async Task SaveAsync()
    {
        if (_validUpto is null)
        {
            Snackbar.Add("Pick a valid-until date.", Severity.Error);
            return;
        }

        _isSubmitting = true;
        try
        {
            var result = await TenantService.AdjustValidityAsync(
                new AdjustTenantValidityRequest(TenantId, _validUpto.Value));
            Snackbar.Add("Validity adjusted.", Severity.Success);
            Dialog.Close(result);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Adjust failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void Cancel() => Dialog.Cancel();
}
