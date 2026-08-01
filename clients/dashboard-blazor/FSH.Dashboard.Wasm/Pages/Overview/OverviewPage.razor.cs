using FSH.BlazorShared.Models;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;

namespace FSH.Dashboard.Wasm.Pages.Overview;

public sealed partial class OverviewPage
{
    [Inject] private IBillingService BillingService { get; set; } = default!;

    private int? _planTotal;
    private int? _activePlans;
    private int? _invoiceTotal;
    private int _invoiceLedgerTotal;
    private int _outstandingCount;
    private string? _error;

    private string Display(int? value)
        => value?.ToString("N0") ?? "-";

    protected override async Task OnInitializedAsync()
    {
        var plansTask = BillingService.GetPlansAsync(includeInactive: true);
        var invoicesTask = BillingService.GetInvoicesAsync(pageNumber: 1, pageSize: 50);

        try
        {
            var plans = await plansTask;
            _planTotal = plans.Count;
            _activePlans = plans.Count(p => p.IsActive);
        }
        catch (Exception ex)
        {
            _error = $"Failed to load plan stats: {ex.Message}";
        }

        try
        {
            var invoices = await invoicesTask;
            _invoiceTotal = invoices.Items.Count;
            _invoiceLedgerTotal = invoices.TotalCount;
            _outstandingCount = invoices.Items.Count(i => i.Status == "Issued");
        }
        catch (Exception ex)
        {
            _error = $"{_error}{Environment.NewLine}Failed to load invoice stats: {ex.Message}".TrimStart('\n');
        }
    }
}
