using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Billing;

public sealed partial class TopupsListPage
{
    private const int PageSize = 20;

    private static readonly string[] Statuses = ["Pending", "Invoiced", "Completed", "Rejected"];

    [Inject] private IBillingService BillingService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private List<TopupRequestDto> _items = [];
    private PagedResult<TopupRequestDto>? _data;
    private string? _error;
    private bool _loading = true;

    private int _pageNumber = 1;
    private string _tenantFilter = string.Empty;
    private string _statusFilter = "Pending";

    private int _pendingCount;
    private decimal _requestedTotal;
    private string _currency = "USD";
    private bool _isDeciding;

    private bool FiltersDirty =>
        !string.IsNullOrWhiteSpace(_tenantFilter) || _statusFilter != "Pending";

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _data = await BillingService.GetTopupRequestsAsync(
                _pageNumber,
                PageSize,
                tenantId: string.IsNullOrWhiteSpace(_tenantFilter) ? null : _tenantFilter.Trim(),
                status: string.IsNullOrEmpty(_statusFilter) ? null : _statusFilter);
            _items = _data.Items;
            _pendingCount = 0;
            _requestedTotal = 0;
            _currency = _items.Count == 0 ? "USD" : _items[0].Currency;
            foreach (var req in _items)
            {
                _requestedTotal += req.Amount;
                if (req.Status == "Pending")
                {
                    _pendingCount++;
                }
            }
        }
        catch (Exception ex)
        {
            _error = $"Failed to load top-up requests: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task OnFilterChangedAsync()
    {
        _pageNumber = 1;
        await LoadAsync();
    }

    private async Task ClearFiltersAsync()
    {
        _tenantFilter = string.Empty;
        _statusFilter = "Pending";
        _pageNumber = 1;
        await LoadAsync();
    }

    private async Task OnPageChangedAsync(int page)
    {
        _pageNumber = Math.Max(1, page);
        await LoadAsync();
    }

    private async Task OpenDecisionAsync(TopupRequestDto request, bool isApprove)
    {
        var mode = isApprove ? "approve" : "reject";
        var parameters = new DialogParameters
        {
            { "Request", request },
            { "Mode", mode },
        };
        var dialog = await DialogService.ShowAsync<TopupDecisionDialog>(
            mode == "reject" ? "Reject top-up request" : "Approve top-up request",
            parameters,
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true });
        _isDeciding = true;
        var result = await dialog.Result;
        _isDeciding = false;
        if (result is null || result.Canceled)
        {
            return;
        }

        if (mode == "reject")
        {
            Snackbar.Add("Request rejected", Severity.Success);
        }
        else if (result.Data is Guid invoiceId)
        {
            Snackbar.Add("Invoice generated", Severity.Success, config =>
            {
                config.Action = "View invoice";
                config.ActionColor = Color.Primary;
                config.OnClick = _ =>
                {
                    Nav.NavigateTo($"/billing/invoices/{invoiceId}");
                    return Task.CompletedTask;
                };
            });
        }

        await LoadAsync();
    }

    private static Color StatusTone(string status) => status switch
    {
        "Completed" => Color.Success,
        "Invoiced" => Color.Info,
        "Pending" => Color.Warning,
        "Rejected" => Color.Error,
        _ => Color.Default,
    };
}
