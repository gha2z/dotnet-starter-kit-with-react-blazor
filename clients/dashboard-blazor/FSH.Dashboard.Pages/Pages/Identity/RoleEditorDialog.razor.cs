using System.ComponentModel.DataAnnotations;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Identity;

public sealed partial class RoleEditorDialog
{
    private static readonly string[] SystemRoleNames = ["Admin", "Basic"];

    public sealed class RoleEditorForm
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

    [Parameter] public RoleDto? Role { get; set; }

    private MudForm? _mudForm;
    private readonly RoleEditorForm _form = new();
    private bool _isSaving;

    private bool IsSystemRole =>
        Role?.Name is not null && SystemRoleNames.Contains(Role.Name, StringComparer.OrdinalIgnoreCase);

    protected override void OnInitialized()
    {
        if (Role is not null)
        {
            _form.Name = Role.Name ?? string.Empty;
            _form.Description = Role.Description;
        }
    }

    private async Task SaveAsync()
    {
        await _mudForm!.ValidateAsync();
        if (!_mudForm.IsValid)
        {
            return;
        }

        _isSaving = true;
        try
        {
            var result = await RoleService.UpsertAsync(new UpsertRoleRequest(
                Role?.Id ?? string.Empty,
                _form.Name,
                string.IsNullOrWhiteSpace(_form.Description) ? null : _form.Description));
            Dialog.Close(result);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to save role: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private void Cancel() => Dialog.Cancel();
}
