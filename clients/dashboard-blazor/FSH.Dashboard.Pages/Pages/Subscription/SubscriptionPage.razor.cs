using FSH.BlazorShared.Formatting;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Models.Dashboard;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Subscription;

public sealed partial class SubscriptionPage
{
    [Inject] private IDashboardService DashboardService { get; set; } = default!;
    [Inject] private IBillingService BillingService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private TenantStatusDto? _status;
    private SubscriptionDto? _subscription;
    private List<UsageSnapshotDto> _usage = new();
    private List<InvoiceDto> _invoices = new();

    private bool _loadingStatus = true;
    private bool _loadingSubscription = true;
    private bool _loadingUsage = true;
    private bool _loadingInvoices = true;

    private string? _statusError;
    private string? _subscriptionError;
    private bool _usageError;
    private bool _invoicesError;

    private string? PlanName => _status?.Plan ?? _subscription?.PlanKey;

    protected override async Task OnInitializedAsync()
    {
        // All four queries are independent — fire them together (React parity: parallel useQuery).
        var statusTask = LoadStatusAsync();
        var subscriptionTask = LoadSubscriptionAsync();
        var usageTask = LoadUsageAsync();
        var invoicesTask = LoadInvoicesAsync();

        await Task.WhenAll(statusTask, subscriptionTask, usageTask, invoicesTask);
    }

    private async Task LoadStatusAsync()
    {
        try
        {
            _status = await DashboardService.GetMyTenantStatusAsync();
        }
        catch (Exception ex)
        {
            _statusError = ex.Message;
        }
        finally
        {
            _loadingStatus = false;
        }
    }

    private async Task LoadSubscriptionAsync()
    {
        try
        {
            _subscription = await DashboardService.GetMySubscriptionAsync();
        }
        catch (Exception ex)
        {
            _subscriptionError = ex.Message;
        }
        finally
        {
            _loadingSubscription = false;
        }
    }

    private async Task LoadUsageAsync()
    {
        try
        {
            _usage = await DashboardService.GetUsageSnapshotsAsync();
        }
        catch
        {
            _usageError = true;
        }
        finally
        {
            _loadingUsage = false;
        }
    }

    private async Task LoadInvoicesAsync()
    {
        try
        {
            var page = await BillingService.GetInvoicesAsync(pageNumber: 1, pageSize: 5);
            _invoices = page.Items
                .OrderByDescending(i => i.CreatedAtUtc)
                .ToList();
        }
        catch
        {
            _invoicesError = true;
        }
        finally
        {
            _loadingInvoices = false;
        }
    }

    private string? ErrorMessage => _statusError ?? _subscriptionError;

    // ---- usage helpers (React parity: toUsageRows — current month, desc utilization) ----

    private IReadOnlyList<UsageRowVm> UsageRows
    {
        get
        {
            var now = DateTime.UtcNow;
            return _usage
                .Where(s => s.PeriodYear == now.Year && s.PeriodMonth == now.Month)
                .Select(s => new UsageRowVm(
                    s.Resource,
                    s.UsedUnits,
                    s.LimitUnits,
                    s.Overage,
                    s.LimitUnits <= 0 ? 0 : (int)Math.Min(100, s.UsedUnits * 100.0 / s.LimitUnits)))
                .OrderByDescending(r => r.Utilization)
                .ToList();
        }
    }

    private sealed record UsageRowVm(string Resource, long Used, long Limit, long Overage, int Utilization);

    // ---- validity helpers (React parity: expiryTone) ----

    private (Color Tone, string Label) ExpiryState() => _status?.ExpiryState switch
    {
        "InGrace" => (Color.Warning, "In grace"),
        "Expired" => (Color.Error, "Expired"),
        "Active" => (Color.Success, "Active"),
        _ => (Color.Default, "Unknown"),
    };

    private static Color InvoiceStatusTone(string status) => status switch
    {
        "Paid" => Color.Success,
        "Issued" => Color.Info,
        "Void" => Color.Error,
        _ => Color.Default,
    };

    private static string FormatPeriod(InvoiceDto invoice) => $"{invoice.PeriodYear}-{invoice.PeriodMonth:00}";

    private static string OverageLabel(long overage) => $"+{FshFormat.Number(overage)}";

    private void OpenInvoice(Guid id) => Navigation.NavigateTo($"/invoices/{id}");

    private void NavigateToInvoices() => Navigation.NavigateTo("/invoices");
}
