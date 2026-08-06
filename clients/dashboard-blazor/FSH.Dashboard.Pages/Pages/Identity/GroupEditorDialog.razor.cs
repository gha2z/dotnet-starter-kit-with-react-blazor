using System.ComponentModel.DataAnnotations;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Identity;

public sealed partial class GroupEditorDialog
{
    public sealed class GroupEditorForm
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(64, MinimumLength = 2, ErrorMessage = "Name must be 2-64 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(256, ErrorMessage = "Keep under 256 characters.")]
        public string? Description { get; set; }

        public bool IsDefault { get; set; }

        public List<string> RoleIds { get; set; } = [];
    }

    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = default!;
    [Inject] private IGroupService GroupService { get; set; } = default!;
    [Inject] private IRoleService RoleService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private MudForm? _mudForm;
    private readonly GroupEditorForm _form = new();
    private List<RoleDto>? _roles;
    private bool _isSubmitting;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _roles = await RoleService.ListAsync();
        }
        catch
        {
            _roles = [];
        }
    }

    private void ToggleRole(string roleId, bool value)
    {
        if (value)
        {
            if (!_form.RoleIds.Contains(roleId))
            {
                _form.RoleIds.Add(roleId);
            }
        }
        else
        {
            _form.RoleIds.Remove(roleId);
        }
    }

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
            var created = await GroupService.CreateAsync(new CreateGroupRequest(
                _form.Name,
                string.IsNullOrWhiteSpace(_form.Description) ? null : _form.Description,
                _form.IsDefault,
                _form.RoleIds));
            Dialog.Close(created);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to create group: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void Cancel() => Dialog.Cancel();
}
