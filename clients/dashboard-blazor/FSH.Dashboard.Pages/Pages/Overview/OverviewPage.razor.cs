using System.Globalization;
using System.Security.Claims;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Audits;
using FSH.BlazorShared.Models.Dashboard;
using FSH.BlazorShared.Services;
using FSH.BlazorShared.Sse;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Overview;

public sealed partial class OverviewPage : IDisposable
{
    private const int LiveFeedCap = 5;

    [Inject] private IDashboardService DashboardService { get; set; } = default!;
    [Inject] private ISseService SseService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthProvider { get; set; } = default!;

    // Data
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

    // Greeting
    private string _greeting = "Overview";
    private string _dateCaption = DateTime.Now.ToString("dddd, MMMM d, yyyy");
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
            LoadRecentAuditsAsync()
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

        _dateCaption = DateTime.Now.ToString("dddd, MMMM d, yyyy");
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
                LoadRecentAuditsAsync()
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

    // ── Stat card views (React parity: clients/dashboard/src/pages/overview.tsx) ──

    private bool _loadingPlan => _loadingSubscription;

    private string PlanValue => _subscriptionError is not null || _subscription is null ? "—" : _subscription.PlanKey;

    private string PlanSub => _subscriptionError is not null ? "Unavailable" : _subscription is null ? "No subscription" : _subscription.Status;

    /// <summary>
    /// Tenant expiry view for the "Valid for" card — same source of truth the
    /// Subscription page reads (validUpto / graceEndsUtc), so an in-grace or
    /// expired tenant sees the warning/danger tone instead of a healthy count.
    /// </summary>
    private (string Value, string Sub, Color Tone) ValidityView()
    {
        if (_tenantError is not null || _tenantStatus is null)
        {
            return ("—", "Status unavailable", Color.Primary);
        }

        return _tenantStatus.ExpiryState switch
        {
            "Expired" => ("Expired", "Contact your operator to renew", Color.Error),
            "InGrace" when string.IsNullOrWhiteSpace(_tenantStatus.GraceEndsUtc)
                => ("0", "in grace period", Color.Warning),
            "InGrace" => (DaysUntil(_tenantStatus.GraceEndsUtc).ToString("N0"),
                          $"grace ends {FormatShortDate(_tenantStatus.GraceEndsUtc)}", Color.Warning),
            _ when string.IsNullOrWhiteSpace(_tenantStatus.ValidUpto)
                => ("Open-ended", "no end date", Color.Success),
            _ => (DaysUntil(_tenantStatus.ValidUpto).ToString("N0"),
                  $"until {FormatShortDate(_tenantStatus.ValidUpto)}", Color.Success),
        };
    }

    private static int DaysUntil(string isoUtc)
    {
        if (!DateTimeOffset.TryParse(isoUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var target))
        {
            return 0;
        }

        return Math.Max(0, (int)Math.Ceiling((target.UtcDateTime - DateTime.UtcNow).TotalDays));
    }

    private static string FormatShortDate(string isoUtc) =>
        DateTimeOffset.TryParse(isoUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto)
            ? dto.UtcDateTime.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)
            : isoUtc;

    /// <summary>React parity: usage rows are the current calendar month only.</summary>
    private List<UsageSnapshotDto> CurrentMonthUsage
    {
        get
        {
            var now = DateTime.UtcNow;
            return _usageSnapshots
                .Where(s => s.PeriodYear == now.Year && s.PeriodMonth == now.Month)
                .OrderByDescending(s => s.LimitUnits > 0 ? (double)s.UsedUnits / s.LimitUnits : 0)
                .ToList();
        }
    }

    private string ResourcesValue => _loadingUsage ? string.Empty : CurrentMonthUsage.Count.ToString("N0");

    private string ResourcesSub
    {
        get
        {
            if (_loadingUsage)
            {
                return string.Empty;
            }

            if (_usageError is not null)
            {
                return "Unavailable";
            }

            var rows = CurrentMonthUsage;
            if (rows.Count == 0)
            {
                return "no current-month activity";
            }

            var avg = (int)Math.Round(rows.Average(s => s.LimitUnits > 0 ? Math.Min(100, (double)s.UsedUnits * 100 / s.LimitUnits) : 0));
            var overage = rows.Sum(s => s.Overage);
            return overage > 0 ? $"{avg}% avg utilization · {overage:N0} overage" : $"{avg}% avg utilization";
        }
    }

    private long UsageOverage => CurrentMonthUsage.Sum(s => s.Overage);

    private static double UsagePercent(UsageSnapshotDto snapshot) =>
        snapshot.LimitUnits > 0 ? Math.Min(100, (double)snapshot.UsedUnits * 100 / snapshot.LimitUnits) : 0;

    private static string UsageFillColor(UsageSnapshotDto snapshot)
    {
        if (snapshot.Overage > 0)
        {
            return "var(--mud-palette-error)";
        }

        return UsagePercent(snapshot) >= 80 ? "var(--mud-palette-warning)" : "var(--mud-palette-primary)";
    }

    private string LiveEventsValue => _sseEventCount.ToString("N0");

    private string LiveStatusText => _isSseConnected ? "connected" : "offline";

    private string LiveStatusColor => _isSseConnected ? "var(--mud-palette-success)" : "var(--mud-palette-error)";

    private double? SubscriptionProgress
    {
        get
        {
            if (_subscription is null || !_subscription.EndUtc.HasValue)
            {
                return null;
            }

            var end = _subscription.EndUtc.Value;
            if (end <= _subscription.StartUtc)
            {
                return null;
            }

            return Math.Clamp((DateTime.UtcNow - _subscription.StartUtc).TotalDays / (end - _subscription.StartUtc).TotalDays, 0, 1);
        }
    }

    private int? SubscriptionDaysLeft => _subscription?.EndUtc is { } end
        ? Math.Max(0, (int)Math.Ceiling((end - DateTime.UtcNow).TotalDays))
        : null;

    // ── Shared tone helpers ──

    private static string ToneCss(Color tone) => tone switch
    {
        Color.Warning => "var(--mud-palette-warning)",
        Color.Error => "var(--mud-palette-error)",
        Color.Success => "var(--mud-palette-success)",
        Color.Info => "var(--mud-palette-info)",
        _ => "var(--mud-palette-primary)",
    };

    private static string ToneBgCss(string toneColor) => $"color-mix(in srgb, {toneColor} 10%, transparent)";

    private static string EventToneColor(string type)
    {
        var t = type.ToLowerInvariant();
        if (t.Contains("fail") || t.Contains("error") || t.Contains("revoke")) return "var(--mud-palette-error)";
        if (t.Contains("warn") || t.Contains("retry")) return "var(--mud-palette-warning)";
        if (t.Contains("login") || t.Contains("issued") || t.Contains("created")) return "var(--mud-palette-success)";
        if (t.Contains("token") || t.Contains("auth")) return "var(--mud-palette-info)";
        return "var(--mud-palette-text-secondary)";
    }

    // ── Audit helpers ──

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

    /// <summary>React parity: severity-tinted icon background for audit rows.</summary>
    private static string AuditSeverityCssColor(AuditSeverity severity) => severity switch
    {
        AuditSeverity.Critical => "var(--mud-palette-error)",
        AuditSeverity.Error => "var(--mud-palette-error)",
        AuditSeverity.Warning => "var(--mud-palette-warning)",
        _ => "var(--mud-palette-info)",
    };

    private static string FormatClock(DateTime dt) => dt.ToString("HH:mm:ss");

    private void NavigateToSubscription()
    {
        Navigation.NavigateTo("/subscription");
    }

    private void NavigateToAudits()
    {
        Navigation.NavigateTo("/system/audits");
    }

    private void NavigateToActivity()
    {
        Navigation.NavigateTo("/activity");
    }

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