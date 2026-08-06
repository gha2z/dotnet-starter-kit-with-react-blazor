using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Identity;

public sealed partial class AddGroupMembersDialog
{
    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = default!;
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IGroupService GroupService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Parameter] public Guid GroupId { get; set; }

    private string _search = string.Empty;
    private List<UserDto> _results = [];
    private List<string> _selected = [];
    private List<string> _existingMemberIds = [];
    private bool _isSearching;
    private bool _isSubmitting;
    private string? _error;
    private CancellationTokenSource? _debounceCts;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var members = await GroupService.GetMembersAsync(GroupId);
            _existingMemberIds = [.. members.Select(m => m.UserId)];
        }
        catch (Exception ex)
        {
            _error = $"Failed to load current members: {ex.Message}";
        }
    }

    private async Task OnSearchChangedAsync(string value)
    {
        _search = value;
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        await Task.Delay(250, token);
        await SearchAsync(token);
    }

    private async Task SearchAsync(CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(_search))
        {
            _results = [];
            return;
        }

        _isSearching = true;
        _error = null;
        try
        {
            var page = await UserService.SearchAsync(new SearchRequest(
                PageNumber: 1,
                PageSize: 25,
                Search: _search.Trim()), token);
            _results = page.Items.ToList();
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
            {
                _error = ex.Message;
            }
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                _isSearching = false;
            }
        }
    }

    private void ToggleUser(string? userId, bool value)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        if (value)
        {
            if (!_selected.Contains(userId))
            {
                _selected.Add(userId);
            }
        }
        else
        {
            _selected.Remove(userId);
        }
    }

    private async Task SaveAsync()
    {
        _isSubmitting = true;
        try
        {
            var result = await GroupService.AddUsersAsync(GroupId, _selected);
            Dialog.Close(result);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to add members: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void Cancel() => Dialog.Cancel();

    private static string DisplayName(UserDto user)
    {
        var fullName = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        return !string.IsNullOrEmpty(fullName) ? fullName : user.UserName ?? user.Email ?? "Unknown user";
    }
}
