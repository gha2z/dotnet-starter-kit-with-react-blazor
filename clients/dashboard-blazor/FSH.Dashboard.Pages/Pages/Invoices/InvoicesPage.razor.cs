using FSH.BlazorShared.Formatting;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Invoices;

public sealed partial class InvoicesPage
{
    [Inject] private IBillingService BillingService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private List<InvoiceDto> _invoices = new();
    private int _pageNumber = 1;
    private int _totalCount;
    private int _totalPages = 1;
    private string _search = string.Empty;
    private const int PageSize = 20;

    private bool _loading = true;
    private string? _error;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            var page = await BillingService.GetMyInvoicesAsync(pageNumber: _pageNumber, pageSize: PageSize);
            _invoices = page.Items
                .OrderByDescending(i => i.CreatedAtUtc)
                .ToList();
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

    private bool SearchActive => !string.IsNullOrWhiteSpace(_search);

    private IReadOnlyList<InvoiceDto> Filtered
    {
        get
        {
            if (!SearchActive)
            {
                return _invoices;
            }

            var term = _search.Trim().ToLowerInvariant();
            return _invoices.Where(i =>
                i.InvoiceNumber.ToLowerInvariant().Contains(term)
                || i.Status.ToLowerInvariant().Contains(term)
                || FormatPeriod(i).Contains(term, StringComparison.Ordinal)).ToList();
        }
    }

    private async Task GoToPage(int page)
    {
        _pageNumber = Math.Clamp(page, 1, _totalPages);
        await LoadAsync();
    }

    private void ClearSearch()
    {
        _search = string.Empty;
    }

    private void OpenInvoice(Guid id) => Navigation.NavigateTo($"/invoices/{id}");

    private static string FormatPeriod(InvoiceDto invoice) => $"{invoice.PeriodYear}-{invoice.PeriodMonth:00}";

    private static Color StatusTone(string status) => status switch
    {
        "Paid" => Color.Success,
        "Issued" => Color.Info,
        "Void" => Color.Error,
        _ => Color.Default,
    };

    private static string StatusLabel(string status) => status;

    private string EmptyStateDescription =>
        SearchActive
            ? $"Nothing matches \"{_search.Trim()}\". Try a different term or clear the search."
            : "Once your tenant has been billed for a period, invoices will appear here.";

    private string MetaText =>
        SearchActive
            ? $"{Filtered.Count} invoice{(Filtered.Count == 1 ? "" : "s")} matched on this page"
            : $"Showing {_invoices.Count} of {_totalCount} invoice{(_totalCount == 1 ? "" : "s")}"
              + (_totalPages > 1 ? $" · page {_pageNumber} of {_totalPages}" : "");
}
