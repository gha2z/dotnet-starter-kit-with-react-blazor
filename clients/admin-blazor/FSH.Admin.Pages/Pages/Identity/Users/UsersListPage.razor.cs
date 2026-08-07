using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Identity.Users;

public sealed partial class UsersListPage
{
    private enum Tri { Any, Yes, No }

    private const string ActiveOption = "Active";
    private const string DisabledOption = "Disabled";
    private const string ConfirmedOption = "Confirmed";
    private const string PendingOption = "Pending";

    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IRoleService RoleService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private MudTable<UserDto>? _table;
    private List<RoleDto> _roles = [];
    private string _searchInput = string.Empty;
    private Tri _activeFilter = Tri.Any;
    private Tri _confirmedFilter = Tri.Any;
    private string _roleId = string.Empty;
    private string? _error;
    private string? _roleError;
    private int _totalCount;
    private CancellationTokenSource? _debounceCts;

    private bool _searchActive => !string.IsNullOrWhiteSpace(_searchInput);

    private string HeaderDescription => _totalCount switch
    {
        0 => "Loading the roster…",
        1 => "1 account on this tenant.",
        _ => $"{_totalCount} accounts on this tenant."
    };

    private string ActiveFilterValue => _activeFilter switch
    {
        Tri.Yes => "Active",
        Tri.No => "Disabled",
        _ => string.Empty
    };

    private string ConfirmedFilterValue => _confirmedFilter switch
    {
        Tri.Yes => "Confirmed",
        Tri.No => "Pending",
        _ => string.Empty
    };

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _roles = await RoleService.ListAsync();
        }
        catch (Exception ex)
        {
            _roleError = $"Failed to load roles: {ex.Message}";
        }
    }

    private async Task OnSearchChangedAsync(string value)
    {
        _searchInput = value;
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(250, _debounceCts.Token);
            await ReloadAsync();
        }
        catch (TaskCanceledException)
        {
        }
    }

    private Task OnActiveFilterChanged(string value)
    {
        _activeFilter = value switch
        {
            "Active" => Tri.Yes,
            "Disabled" => Tri.No,
            _ => Tri.Any
        };
        return ReloadAsync();
    }

    private Task OnConfirmedFilterChanged(string value)
    {
        _confirmedFilter = value switch
        {
            "Confirmed" => Tri.Yes,
            "Pending" => Tri.No,
            _ => Tri.Any
        };
        return ReloadAsync();
    }

    private Task OnRoleFilterChanged(string value)
    {
        _roleId = value;
        return ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (_table is not null)
        {
            await _table.ReloadServerData();
        }
    }

    private async Task<TableData<UserDto>> LoadDataAsync(TableState state, CancellationToken ct)
    {
        try
        {
            var request = new FSH.BlazorShared.Models.SearchRequest(
                PageNumber: state.Page + 1,
                PageSize: state.PageSize,
                Search: _searchInput,
                SortBy: state.SortLabel,
                SortDirection: state.SortDirection == SortDirection.Descending ? "desc" : "asc",
                Filters: new Dictionary<string, string?>
                {
                    ["IsActive"] = _activeFilter switch { Tri.Yes => "true", Tri.No => "false", _ => null },
                    ["EmailConfirmed"] = _confirmedFilter switch { Tri.Yes => "true", Tri.No => "false", _ => null },
                    ["RoleId"] = string.IsNullOrEmpty(_roleId) ? null : _roleId,
                });

            var result = await UserService.SearchAsync(request, ct);
            _totalCount = result.TotalCount;
            _error = null;
            await InvokeAsync(StateHasChanged);
            return new TableData<UserDto> { Items = result.Items, TotalItems = result.TotalCount };
        }
        catch (OperationCanceledException)
        {
            return new TableData<UserDto> { Items = [], TotalItems = 0 };
        }
        catch (Exception ex)
        {
            _error = $"Failed to load users: {ex.Message}";
            await InvokeAsync(StateHasChanged);
            return new TableData<UserDto> { Items = [], TotalItems = 0 };
        }
    }

    private async Task RowClickAsync(TableRowClickEventArgs<UserDto> args)
    {
        if (args.Item?.Id is { } id)
        {
            Nav.NavigateTo($"/users/{id}");
        }
    }

    private static string RowClassFunc(UserDto user, int rowIndex) => "cursor-pointer";

    private async Task OpenCreateDialogAsync()
    {
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<UserCreateDialog>("New user", options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled && result.Data is RegisterUserResponse response && response.UserId is not null)
        {
            Nav.NavigateTo($"/users/{response.UserId}");
        }
    }

    private static string DisplayName(UserDto user)
    {
        var fullName = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        return !string.IsNullOrEmpty(fullName) ? fullName : user.UserName ?? user.Email ?? "Unnamed";
    }


}

