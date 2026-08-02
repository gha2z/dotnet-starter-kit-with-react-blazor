using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Billing;

public sealed partial class InvoicesListPage
{
    private const int PageSize = 20;

    private static readonly string[] Statuses = ["Draft", "Issued", "Paid", "Void"];

    [Inject] private IBillingService BillingService { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private List<InvoiceDto> _items = [];
    private PagedResult<InvoiceDto>? _data;
    private string? _error;
    private bool _loading = true;

    private int _pageNumber = 1;
    private string _tenantFilter = string.Empty;
    private string _statusFilter = string.Empty;
    private string _periodYear = string.Empty;
    private string _periodMonth = string.Empty;

    private decimal _totalBilled;
    private decimal _outstanding;
    private decimal _paidTotal;
    private int _paidCount;
    private string _currency = "USD";

    private bool FiltersDirty =>
        !string.IsNullOrWhiteSpace(_tenantFilter)
        || !string.IsNullOrEmpty(_statusFilter)
        || !string.IsNullOrWhiteSpace(_periodYear)
        || !string.IsNullOrWhiteSpace(_periodMonth);

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
            _data = await BillingService.GetInvoicesAsync(
                _pageNumber,
                PageSize,
                tenantId: string.IsNullOrWhiteSpace(_tenantFilter) ? null : _tenantFilter.Trim(),
                status: string.IsNullOrEmpty(_statusFilter) ? null : _statusFilter,
                periodYear: ParseIntOrNull(_periodYear),
                periodMonth: ParseIntOrNull(_periodMonth));
            _items = _data.Items;
            ComputeTotals();
        }
        catch (Exception ex)
        {
            _error = $"Failed to load invoices: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private void ComputeTotals()
    {
        _totalBilled = 0;
        _outstanding = 0;
        _paidTotal = 0;
        _paidCount = 0;
        _currency = _items.Count == 0 ? "USD" : _items[0].Currency;
        foreach (var inv in _items)
        {
            _totalBilled += inv.SubtotalAmount;
            if (inv.Status == "Paid")
            {
                _paidTotal += inv.SubtotalAmount;
                _paidCount++;
            }
            else if (inv.Status == "Issued")
            {
                _outstanding += inv.SubtotalAmount;
            }
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
        _statusFilter = string.Empty;
        _periodYear = string.Empty;
        _periodMonth = string.Empty;
        _pageNumber = 1;
        await LoadAsync();
    }

    private async Task OnPageChangedAsync(int page)
    {
        _pageNumber = Math.Max(1, page);
        await LoadAsync();
    }

    private static int? ParseIntOrNull(string value) =>
        int.TryParse(value, out var parsed) ? parsed : null;

    private static Color StatusTone(string status) => status switch
    {
        "Paid" => Color.Success,
        "Issued" => Color.Info,
        "Draft" => Color.Warning,
        "Void" => Color.Error,
        _ => Color.Default,
    };
}
