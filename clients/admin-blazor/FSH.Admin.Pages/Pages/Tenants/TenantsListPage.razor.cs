using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Tenants;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Tenants;

public sealed partial class TenantsListPage
{
    [Inject] private ITenantService TenantService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private MudTable<TenantDto>? _table;
    private string? _error;
    private int _totalCount;

    private string HeaderDescription => _totalCount switch
    {
        0 => "Loading the registry…",
        1 => "1 tenant registered on this instance.",
        _ => $"{_totalCount} tenants registered on this instance."
    };

    private async Task<TableData<TenantDto>> LoadDataAsync(TableState state, CancellationToken ct)
    {
        try
        {
            var request = new SearchRequest(
                PageNumber: state.Page + 1,
                PageSize: state.PageSize,
                SortBy: state.SortLabel);
            var result = await TenantService.SearchAsync(request, ct);
            _totalCount = result.TotalCount;
            _error = null;
            await InvokeAsync(StateHasChanged);
            return new TableData<TenantDto> { Items = result.Items, TotalItems = result.TotalCount };
        }
        catch (OperationCanceledException)
        {
            return new TableData<TenantDto> { Items = [], TotalItems = 0 };
        }
        catch (Exception ex)
        {
            _error = $"Failed to load tenants: {ex.Message}";
            await InvokeAsync(StateHasChanged);
            return new TableData<TenantDto> { Items = [], TotalItems = 0 };
        }
    }

    private async Task RowClickAsync(TableRowClickEventArgs<TenantDto> args)
    {
        if (args.Item?.Id is { } id)
        {
            Nav.NavigateTo($"/tenants/{id}");
        }
    }

    private static string RowClassFunc(TenantDto tenant, int rowIndex) => "cursor-pointer";

    private async Task OpenCreateDialogAsync()
    {
        var options = new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<TenantCreateDialog>("New tenant", options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled && result.Data is CreateTenantResponse response)
        {
            Snackbar.Add(
                $"Tenant {response.Id} created — provisioning runs in the background.",
                Severity.Success);
            Nav.NavigateTo($"/tenants/{response.Id}");
        }
    }

    private static string FormatDate(DateTime value) => value.ToString("MMM d, yyyy");
}
