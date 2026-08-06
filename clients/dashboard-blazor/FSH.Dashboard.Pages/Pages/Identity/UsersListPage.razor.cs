using FSH.BlazorShared.Components;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Identity;

public sealed partial class UsersListPage
{
    private const int PageSize = 20;
    private const string SortDefault = "username";

    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IRoleService RoleService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private List<UserDto> _items = [];
    private List<RoleDto> _roles = [];
    private int _pageNumber = 1;    private int _totalCount;
    private int _totalPages = 1;
    private string _search = string.Empty;
    private string? _statusFilter;
    private string? _emailFilter;
    private string? _roleFilter;

    private bool _loading = true;
    private string? _error;
    private string? _roleError;
    private CancellationTokenSource? _debounceCts;

    private bool SearchActive => !string.IsNullOrWhiteSpace(_search);
    private bool FilterActive => SearchActive || _statusFilter is not null || _emailFilter is not null || !string.IsNullOrEmpty(_roleFilter);

    private string MetaText => $"{_totalCount} user{(_totalCount == 1 ? string.Empty : "s")} found";

    private string EmptyStateDescription => FilterActive
        ? SearchActive
            ? $"Nothing matches \"{_search.Trim()}\". Try a different term or clear the filters."
            : "No users match the current filters."
        : "Register the first member to seed this tenant. They'll receive a confirmation email if email confirmation is enabled.";

    protected override async Task OnInitializedAsync()
    {
        await LoadRolesAsync();
        await LoadAsync();
    }

    private async Task LoadRolesAsync()
    {
        try
        {
            _roles = await RoleService.ListAsync();
        }
        catch (Exception ex)
        {
            _roleError = $"Failed to load roles for the filter: {ex.Message}";
        }
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            var filters = new Dictionary<string, string?>
            {
                ["IsActive"] = _statusFilter switch
                {
                    StatusActive => "true",
                    StatusInactive => "false",
                    _ => null,
                },
                ["EmailConfirmed"] = _emailFilter switch
                {
                    EmailConfirmed => "true",
                    EmailPending => "false",
                    _ => null,
                },
                ["RoleId"] = string.IsNullOrEmpty(_roleFilter) ? null : _roleFilter,
            };

            var page = await UserService.SearchAsync(new SearchRequest(
                PageNumber: _pageNumber,
                PageSize: PageSize,
                Search: SearchActive ? _search.Trim() : null,
                SortBy: SortDefault,
                Filters: filters));
            _items = page.Items;
            _totalCount = page.TotalCount;
            _totalPages = page.TotalPages;
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task OnSearchChangedAsync(string value)
    {
        _search = value;
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        try
        {
            await Task.Delay(250, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

        _pageNumber = 1;
        await LoadAsync();
    }

    private async Task GoToPageAsync(int page)
    {
        _pageNumber = Math.Clamp(page, 1, _totalPages);
        await LoadAsync();
    }

    private void ClearFilters()
    {
        _search = string.Empty;
        _statusFilter = null;
        _emailFilter = null;
        _roleFilter = null;
        _pageNumber = 1;
        _ = LoadAsync();
    }

    private void OpenDetail(UserDto user) => Nav.NavigateTo($"/identity/users/{user.Id}");

    private async Task OpenCreateAsync()
    {
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<UserCreateDialog>(null, options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled && result.Data is RegisterUserResponse created)
        {
            Snackbar.Add(created.Message ?? $"User {created.UserId} registered", Severity.Success);
            _pageNumber = 1;
            await LoadAsync();
        }
    }

    private static string DisplayName(UserDto user)
    {
        var fullName = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        return !string.IsNullOrEmpty(fullName) ? fullName : user.UserName ?? user.Email ?? "Unnamed user";
    }
}
