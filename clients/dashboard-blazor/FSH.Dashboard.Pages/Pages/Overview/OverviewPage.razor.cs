using FSH.BlazorShared.Models;
using FSH.BlazorShared.Services;
using FSH.BlazorShared.Sse;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using FSH.BlazorShared.Models.Audits;
using FSH.BlazorShared.Models.Dashboard;

namespace FSH.Dashboard.Wasm.Pages.Overview;

public sealed partial class OverviewPage
{
    [Inject] private IBillingService BillingService { get; set; } = default!;
    [Inject] private IDashboardService DashboardService { get; set; } = default!;
    [Inject] private ISseService SseService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    // Subscription data
    private SubscriptionDto? _subscription;
    private TenantStatusDto? _tenantStatus;
    private List<UsageSnapshotDto> _usageSnapshots = new();
    private List<AuditSummaryDto> _recentAudits = new();
    private bool _isSseConnected;

    // Loading states
    private bool _loadingSubscription = true;
    private bool _loadingTenant = true;
    private bool _loadingUsage = true;
    private bool _loadingAudits = true;

    // Error states
    private string? _subscriptionError;
    private string? _tenantError;
    private string? _usageError;
    private string? _auditError;

    // Stats for overview cards
    private int? _planTotal;
    private int? _activePlans;
    private int? _invoiceTotal;
    private int _invoiceLedgerTotal;
    private int _outstandingCount;

    // Event subscription — the App root owns the SSE connection lifecycle
    // (start/stop on login/logout); this page only observes its state.
    protected override async Task OnInitializedAsync()
    {
        SseService.ConnectionChanged += OnSseConnectionChanged;
        _isSseConnected = SseService.IsConnected;

        // Load all data in parallel
        var loadTasks = new List<Task>
        {
            LoadTenantStatusAsync(),
            LoadSubscriptionAsync(),
            LoadUsageSnapshotsAsync(),
            LoadRecentAuditsAsync(),
            LoadBillingStatsAsync()
        };

        await Task.WhenAll(loadTasks);
    }

    private void OnSseConnectionChanged()
    {
        InvokeAsync(() =>
        {
            _isSseConnected = SseService.IsConnected;
            StateHasChanged();
        });
    }

    private async Task LoadTenantStatusAsync()
    {
        try
        {
            _loadingTenant = true;
            _tenantStatus = await DashboardService.GetMyTenantStatusAsync();
        }
        catch (Exception ex)
        {
            _tenantError = $"Failed to load tenant status: {ex.Message}";
        }
        finally
        {
            _loadingTenant = false;
        }
    }

    private async Task LoadSubscriptionAsync()
    {
        try
        {
            _loadingSubscription = true;
            _subscription = await DashboardService.GetMySubscriptionAsync();
        }
        catch (Exception ex)
        {
            _subscriptionError = $"Failed to load subscription: {ex.Message}";
        }
        finally
        {
            _loadingSubscription = false;
        }
    }

    private async Task LoadUsageSnapshotsAsync()
    {
        try
        {
            _loadingUsage = true;
            _usageSnapshots = await DashboardService.GetUsageSnapshotsAsync();
        }
        catch (Exception ex)
        {
            _usageError = $"Failed to load usage data: {ex.Message}";
        }
        finally
        {
            _loadingUsage = false;
        }
    }

    private async Task LoadRecentAuditsAsync()
    {
        try
        {
            _loadingAudits = true;
            _recentAudits = await DashboardService.GetRecentAuditsAsync(5);
        }
        catch (Exception ex)
        {
            _auditError = $"Failed to load recent audits: {ex.Message}";
        }
        finally
        {
            _loadingAudits = false;
        }
    }

    private async Task LoadBillingStatsAsync()
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
            _subscriptionError = $"Failed to load plan stats: {ex.Message}";
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
            _subscriptionError = $"{_subscriptionError}{Environment.NewLine}Failed to load invoice stats: {ex.Message}".TrimStart('\n');
        }
    }

    private string GetAuditIcon(AuditEventType eventType)
    {
        return eventType switch
        {
            AuditEventType.EntityChange => Icons.Material.Filled.History,
            AuditEventType.Security => Icons.Material.Filled.Security,
            AuditEventType.Activity => Icons.Material.Filled.Notifications,
            AuditEventType.Exception => Icons.Material.Filled.ErrorOutline,
            _ => Icons.Material.Filled.Description
        };
    }

    private string FormatCurrency(decimal? amount)
    {
        return amount?.ToString("C") ?? "-";
    }

    private string FormatNumber(int? number)
    {
        return number?.ToString("N0") ?? "-";
    }

    private string GetSubscriptionStatusText()
    {
        return _subscription?.Status switch
        {
            "Active" => "Active",
            "Suspended" => "Suspended",
            "Cancelled" => "Cancelled",
            _ => "No subscription"
        };
    }

    private Color GetSubscriptionStatusColor()
    {
        return _subscription?.Status switch
        {
            "Active" => Color.Success,
            "Suspended" => Color.Warning,
            "Cancelled" => Color.Error,
            _ => Color.Default
        };
    }

    private string GetTenantStatusText()
    {
        return _tenantStatus?.ExpiryState switch
        {
            "Active" => "Active",
            "InGrace" => "In Grace Period",
            "Expired" => "Expired",
            _ => "Unknown"
        };
    }

    private Color GetTenantStatusColor()
    {
        return _tenantStatus?.ExpiryState switch
        {
            "Active" => Color.Success,
            "InGrace" => Color.Warning,
            "Expired" => Color.Error,
            _ => Color.Default
        };
    }

    private void NavigateToSubscription()
    {
        Navigation.NavigateTo("/subscription");
    }

    private void NavigateToInvoices()
    {
        Navigation.NavigateTo("/invoices");
    }

    private void NavigateToUsage()
    {
        Navigation.NavigateTo("/usage");
    }

    private void NavigateToAudits()
    {
        Navigation.NavigateTo("/system/audits");
    }

    private void NavigateToBilling()
    {
        Navigation.NavigateTo("/billing");
    }

    private void NavigateToWallet()
    {
        Navigation.NavigateTo("/wallet");
    }

    private void NavigateToProfile()
    {
        Navigation.NavigateTo("/settings/profile");
    }

    public void Dispose()
    {
        SseService.ConnectionChanged -= OnSseConnectionChanged;
    }
}