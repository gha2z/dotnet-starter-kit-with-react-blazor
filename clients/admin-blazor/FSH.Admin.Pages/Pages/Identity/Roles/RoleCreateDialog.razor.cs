using System.ComponentModel.DataAnnotations;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Identity.Roles;

public sealed partial class RoleCreateDialog
{
    public sealed class RoleForm
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(64, MinimumLength = 2, ErrorMessage = "Name must be 2-64 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(256, ErrorMessage = "Keep under 256 characters.")]
        public string? Description { get; set; }
    }

    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = default!;
    [Inject] private IRoleService RoleService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private MudForm? _mudForm;
    private readonly RoleForm _form = new();
    private bool _isSubmitting;

    private async Task SaveAsync()
    {
        await _mudForm!.ValidateAsync();
        if (!_mudForm.IsValid)
        {
            return;
        }

        _isSubmitting = true;
        try
        {
            var request = new UpsertRoleRequest(
                string.Empty,
                _form.Name,
                string.IsNullOrWhiteSpace(_form.Description) ? null : _form.Description);
            var result = await RoleService.UpsertAsync(request);
            Dialog.Close(result);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to create role: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void Cancel() => Dialog.Cancel();
}
