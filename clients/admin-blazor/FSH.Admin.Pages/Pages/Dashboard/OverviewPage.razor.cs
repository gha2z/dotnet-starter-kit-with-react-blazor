using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;

namespace FSH.Admin.Wasm.Pages.Dashboard;

public sealed partial class OverviewPage
{
    [Inject] private ITenantService TenantService { get; set; } = default!;
    [Inject] private IBillingService BillingService { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private void GoToTenants() => Nav.NavigateTo("/tenants");

    private void GoToUsers() => Nav.NavigateTo("/users");

    private void GoToRoles() => Nav.NavigateTo("/roles");

    private int? _tenantTotal;
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
        var tenantsTask = TenantService.SearchAsync(new SearchRequest(PageNumber: 1, PageSize: 1));
        var plansTask = BillingService.GetPlansAsync(includeInactive: true);
        var invoicesTask = BillingService.GetInvoicesAsync(pageNumber: 1, pageSize: 50);

        try
        {
            _tenantTotal = (await tenantsTask).TotalCount;
        }
        catch (Exception ex)
        {
            _error = $"Failed to load tenant stats: {ex.Message}";
        }

        try
        {
            var plans = await plansTask;
            _planTotal = plans.Count;
            _activePlans = plans.Count(p => p.IsActive);
        }
        catch (Exception ex)
        {
            _error = $"{_error}{Environment.NewLine}Failed to load plan stats: {ex.Message}".TrimStart('\n');
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
