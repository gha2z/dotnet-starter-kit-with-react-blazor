using FSH.BlazorShared.Components;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Identity;

public sealed partial class GroupDetailPage
{
    [Parameter] public string Id { get; set; } = string.Empty;

    [Inject] private IGroupService GroupService { get; set; } = default!;
    [Inject] private IRoleService RoleService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private GroupDto? _group;
    private List<GroupMemberDto>? _members;
    private List<RoleDto>? _roles;
    private List<string> _selectedRoleIds = [];
    private List<string> _isRemovingMembers = [];

    private string _name = string.Empty;
    private string _description = string.Empty;
    private bool _isDefault;
    private bool _isDirty;

    private bool _loading = true;
    private bool _isSaving;
    private string? _error;

    private bool IsSystemGroup => _group?.IsSystemGroup ?? false;

    private int _dirtyCount => _isDirty ? 1 : 0;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var groupId = Guid.Parse(Id);
            var groupTask = GroupService.GetByIdAsync(groupId);
            var membersTask = GroupService.GetMembersAsync(groupId);
            var rolesTask = RoleService.ListAsync();
            await Task.WhenAll(groupTask, membersTask, rolesTask);

            _group = groupTask.Result;
            _members = membersTask.Result;
            _roles = rolesTask.Result;
            ResetDraft();
        }
        catch (Exception ex)
        {
            _error = $"Failed to load group: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private void ResetDraft()
    {
        if (_group is null)
        {
            return;
        }

        _name = _group.Name;
        _description = _group.Description ?? string.Empty;
        _isDefault = _group.IsDefault;
        _selectedRoleIds = [.. (_group.RoleIds ?? [])];
        _isDirty = false;
    }

    private void ToggleRole(string roleId, bool value)
    {
        if (value)
        {
            if (!_selectedRoleIds.Contains(roleId))
            {
                _selectedRoleIds.Add(roleId);
            }
        }
        else
        {
            _selectedRoleIds.Remove(roleId);
        }

        MarkDirty();
    }

    private void MarkDirty()
    {
        _isDirty = true;
    }

    private async Task SaveAsync()
    {
        _isSaving = true;
        try
        {
            var updated = await GroupService.UpdateAsync(new UpdateGroupRequest(
                _group!.Id,
                _name.Trim(),
                string.IsNullOrWhiteSpace(_description) ? null : _description.Trim(),
                _isDefault,
                _selectedRoleIds));
            _group = updated;
            ResetDraft();
            Snackbar.Add("Group updated", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Update failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task RemoveMemberAsync(GroupMemberDto member)
    {
        _isRemovingMembers.Add(member.UserId);
        try
        {
            await GroupService.RemoveUserAsync(_group!.Id, member.UserId);
            _members!.RemoveAll(m => m.UserId == member.UserId);
            _group = _group with { MemberCount = Math.Max(0, _group.MemberCount - 1) };
            Snackbar.Add("Member removed", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Remove failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isRemovingMembers.Remove(member.UserId);
        }
    }

    private async Task OpenAddMembersAsync()
    {
        var parameters = new DialogParameters<AddGroupMembersDialog>
        {
            { x => x.GroupId, _group!.Id },
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<AddGroupMembersDialog>(string.Empty, parameters, options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled && result.Data is AddUsersToGroupResult addResult)
        {
            if (addResult.AlreadyMemberUserIds.Count > 0)
            {
                Snackbar.Add($"{addResult.AlreadyMemberUserIds.Count} user(s) already in this group", Severity.Warning);
            }

            if (addResult.AddedCount > 0)
            {
                Snackbar.Add($"{addResult.AddedCount} member(s) added", Severity.Success);
                await ReloadMembersAsync();
            }
        }
    }

    private async Task ReloadMembersAsync()
    {
        try
        {
            var groupId = Guid.Parse(Id);
            _members = await GroupService.GetMembersAsync(groupId);
            _group = await GroupService.GetByIdAsync(groupId);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to refresh members: {ex.Message}", Severity.Error);
        }
    }

    private async Task ConfirmDeleteAsync()
    {
        var parameters = new DialogParameters<FshConfirmDialogContent>
        {
            { x => x.Message, $"Delete group \"{_group?.Name}\"? Members keep their direct roles; only the group's role grants are removed." },
            { x => x.ConfirmText, "Delete group" },
        };
        var dialog = await DialogService.ShowAsync<FshConfirmDialogContent>("Delete group", parameters);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        try
        {
            await GroupService.DeleteAsync(_group!.Id);
            Snackbar.Add("Group deleted", Severity.Success);
            Nav.NavigateTo("/identity/groups");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Delete failed: {ex.Message}", Severity.Error);
        }
    }

    private void GoBack() => Nav.NavigateTo("/identity/groups");

    private static string MemberDisplay(GroupMemberDto member)
    {
        var fullName = string.Join(' ', new[] { member.FirstName, member.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        return !string.IsNullOrEmpty(fullName) ? fullName : member.UserName ?? member.Email ?? "Unknown user";
    }
}
