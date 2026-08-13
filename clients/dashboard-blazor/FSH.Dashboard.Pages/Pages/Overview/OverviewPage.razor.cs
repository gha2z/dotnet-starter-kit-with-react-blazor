using System.Text.Json;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Services;
using FSH.BlazorShared.Sse;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using FSH.BlazorShared.Models.Audits;
using FSH.BlazorShared.Models.Dashboard;
using System.Security.Claims;

namespace FSH.Dashboard.Wasm.Pages.Overview;

public sealed partial class OverviewPage : IDisposable
{
    private const int LiveFeedCap = 5;

    [Inject] private IBillingService BillingService { get; set; } = default!;
    [Inject] private IDashboardService DashboardService { get; set; } = default!;
    [Inject] private ISseService SseService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthProvider { get; set; } = default!;

    // Subscription data
    private SubscriptionDto? _subscription;
    private TenantStatusDto? _tenantStatus;
    private List<UsageSnapshotDto> _usageSnapshots = new();
    private List<AuditSummaryDto> _recentAudits = new();
    private bool _isSseConnected;

    // Live feed (React parity: latest 5 SSE events with clock + tone badge)
    private readonly List<LiveEvent> _liveEvents = new();
    private int _sseEventCount;
    private IDisposable? _sseSubscription;
    private bool _disposed;

    // Loading states
    private bool _loadingSubscription = true;
    private bool _loadingTenant = true;
    private bool _loadingUsage = true;
    private bool _loadingAudits = true;
    private bool _refreshing;

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

    // Greeting
    private string _greeting = "Overview";
    private string _dateCaption = DateTime.Now.ToString("dddd, MMMM d");
    private string _tenantLabel = string.Empty;

    // Event subscription — the App root owns the SSE connection lifecycle
    // (start/stop on login/logout); this page only observes its state.
    protected override async Task OnInitializedAsync()
    {
        SseService.ConnectionChanged += OnSseConnectionChanged;
        _sseSubscription = SseService.Messages.Subscribe(new SseObserver(OnSseEvent));
        _isSseConnected = SseService.IsConnected;

        await BuildGreetingAsync();

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

    private async Task BuildGreetingAsync()
    {
        try
        {
            var authState = await AuthProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var firstName = user.FindFirst(ClaimTypes.GivenName)?.Value
                            ?? user.Identity?.Name?.Split(' ').FirstOrDefault()
                            ?? string.Empty;

            var hour = DateTime.Now.Hour;
            var timeOfDay = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";
            _greeting = string.IsNullOrEmpty(firstName) ? $"{timeOfDay}!" : $"{timeOfDay}, {firstName}";
        }
        catch
        {
            _greeting = "Good day!";
        }

        _dateCaption = DateTime.Now.ToString("dddd, MMMM d");
    }

    private void OnSseConnectionChanged()
    {
        InvokeAsync(() =>
        {
            _isSseConnected = SseService.IsConnected;
            StateHasChanged();
        });
    }

    private void OnSseEvent(SseEvent message)
    {
        _sseEventCount++;
        _liveEvents.Insert(0, new LiveEvent(message.EventType, DateTime.Now));

        if (_liveEvents.Count > LiveFeedCap)
        {
            _liveEvents.RemoveAt(_liveEvents.Count - 1);
        }

        InvokeAsync(StateHasChanged);
    }

    private async Task LoadTenantStatusAsync()
    {
        try
        {
            _loadingTenant = true;
            _tenantStatus = await DashboardService.GetMyTenantStatusAsync();
            _tenantLabel = _tenantStatus?.Name ?? "Tenant";
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

    private async Task RefreshAllAsync()
    {
        _refreshing = true;
        StateHasChanged();
        try
        {
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
        finally
        {
            _refreshing = false;
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

    /// <summary>React parity: severity tint on the audit icon background.</summary>
    private Color AuditSeverityTone(AuditSeverity severity) => severity switch
    {
        AuditSeverity.Critical => Color.Error,
        AuditSeverity.Error => Color.Error,
        AuditSeverity.Warning => Color.Warning,
        _ => Color.Info,
    };

    private static string FormatClock(DateTime dt) => dt.ToString("HH:mm:ss");

    /// <summary>React parity: severity-tinted icon background for audit rows.</summary>
    private static string AuditSeverityCssColor(AuditSeverity severity) => severity switch
    {
        AuditSeverity.Critical => "var(--mud-palette-error)",
        AuditSeverity.Error => "var(--mud-palette-error)",
        AuditSeverity.Warning => "var(--mud-palette-warning)",
        _ => "var(--mud-palette-info)",
    };

    private static string TileStyle(string toneColor) => $"border-left: 3px solid {toneColor};";

    private static string TileIconStyle(string toneColor) => $"color: {toneColor}; font-size: 18px;";

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

    private void NavigateToUsers() => Navigation.NavigateTo("/identity/users");

    private void NavigateToCatalog() => Navigation.NavigateTo("/catalog/products");

    private void NavigateToActivity() => Navigation.NavigateTo("/activity");

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        SseService.ConnectionChanged -= OnSseConnectionChanged;
        _sseSubscription?.Dispose();
    }

    private sealed class SseObserver(Action<SseEvent> onNext) : IObserver<SseEvent>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(SseEvent value) => onNext(value);
    }

    private sealed record LiveEvent(string EventType, DateTime ReceivedAt);
}