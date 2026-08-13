using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Audits;
using FSH.BlazorShared.Permissions;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Audits;

public partial class AuditsListPage
{
    private const int PageSize = 25;

    private readonly List<AuditSummaryDto> _items = [];
    private PagedResult<AuditSummaryDto>? _data;
    private AuditSummaryStats? _summary;
    private string? _error;

    private string _searchInput = string.Empty;
    private string _eventTypeFilter = string.Empty;
    private string _severityFilter = string.Empty;
    private string _tenantFilter = string.Empty;
    private string _correlationFilter = string.Empty;

    private bool _loading;
    private bool _canCrossTenant;
    private CancellationTokenSource? _searchDebounceCts;
    private bool _disposed;

    [Inject]
    private IAuditService AuditService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthProvider { get; set; } = default!;

    [Inject]
    private IAuthorizationService AuthorizationService { get; set; } = default!;

    private int ActiveFilterCount
    {
        get
        {
            var count = 0;
            if (!string.IsNullOrWhiteSpace(_eventTypeFilter)) count++;
            if (!string.IsNullOrWhiteSpace(_severityFilter)) count++;
            if (!string.IsNullOrWhiteSpace(_tenantFilter)) count++;
            if (!string.IsNullOrWhiteSpace(_correlationFilter)) count++;
            if (!string.IsNullOrWhiteSpace(_searchInput)) count++;
            return count;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthProvider.GetAuthenticationStateAsync();
        _canCrossTenant = (await AuthorizationService.AuthorizeAsync(
            authState.User, AuditingPermissions.AuditTrails.ViewCrossTenant)).Succeeded;

        _loading = true;
        await LoadSummaryAsync();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            var request = new ListAuditsRequest
            {
                PageNumber = _data?.PageNumber ?? 1,
                PageSize = PageSize,
                Sort = "-OccurredAtUtc",
                EventType = ParseEventType(_eventTypeFilter),
                Severity = ParseSeverity(_severityFilter),
                TenantId = NullIfEmpty(_tenantFilter),
                CorrelationId = NullIfEmpty(_correlationFilter),
                Search = NullIfEmpty(_searchInput),
            };

            _data = await AuditService.ListAsync(request);
            _items.Clear();
            _items.AddRange(_data.Items);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
            _items.Clear();
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task LoadSummaryAsync()
    {
        try
        {
            var summary = await AuditService.GetSummaryAsync(_canCrossTenant ? NullIfEmpty(_tenantFilter) : null);
            _summary = new AuditSummaryStats(summary);
        }
        catch
        {
            _summary = null;
        }
    }

    private async Task OnSearchChangedAsync()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts = new CancellationTokenSource();
        var token = _searchDebounceCts.Token;
        try
        {
            await Task.Delay(250, token);
            if (!token.IsCancellationRequested)
            {
                await ReloadFromPageOneAsync();
            }
        }
        catch (TaskCanceledException)
        {
            // Debounce superseded by a newer keystroke.
        }
    }

    private async Task OnFilterChangedAsync() => await ReloadFromPageOneAsync();

    private async Task OnPageChangedAsync(int pageNumber)
    {
        _data = new PagedResult<AuditSummaryDto>(_items.ToList(), pageNumber, PageSize, _data?.TotalCount ?? 0, _data?.TotalPages ?? 0, pageNumber > 1, pageNumber < (_data?.TotalPages ?? 0));
        await LoadAsync();
    }

    private async Task ReloadFromPageOneAsync()
    {
        if (_data is not null)
        {
            _data = _data with { PageNumber = 1 };
        }

        await LoadAsync();
        await LoadSummaryAsync();
    }

    private async Task ClearFiltersAsync()
    {
        _searchInput = string.Empty;
        _eventTypeFilter = string.Empty;
        _severityFilter = string.Empty;
        _tenantFilter = string.Empty;
        _correlationFilter = string.Empty;
        await ReloadFromPageOneAsync();
    }

    private async Task OpenDetailAsync(Guid id)
    {
        var parameters = new DialogParameters<AuditDetailDialog>
        {
            { x => x.AuditId, id },
        };
        var options = new DialogOptions
        {
            Position = DialogPosition.CenterRight,
            MaxWidth = MaxWidth.Medium,
            CloseButton = true,
            BackgroundClass = "fsh-dialog-backdrop",
        };
        await DialogService.ShowAsync<AuditDetailDialog>(null, parameters, options);
    }

    private static string SeverityDot(AuditSeverity severity) => severity switch
    {
        AuditSeverity.Critical or AuditSeverity.Error => "var(--mud-palette-error)",
        AuditSeverity.Warning => "var(--mud-palette-warning)",
        AuditSeverity.Information => "var(--mud-palette-info)",
        _ => "var(--mud-palette-text-disabled)",
    };

    private static Color EventTypeTone(AuditEventType type) => type switch
    {
        AuditEventType.Security => Color.Info,
        AuditEventType.Exception => Color.Error,
        AuditEventType.EntityChange => Color.Primary,
        AuditEventType.Activity => Color.Default,
        _ => Color.Default,
    };

    private static AuditEventType? ParseEventType(string value) =>
        Enum.TryParse<AuditEventType>(value, true, out var type) ? type : null;

    private static AuditSeverity? ParseSeverity(string value) =>
        Enum.TryParse<AuditSeverity>(value, true, out var severity) ? severity : null;

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
    }

    private sealed record AuditSummaryStats(AuditSummaryAggregateDto Summary)
    {
        public long Total { get; } = Summary.EventsByType.Values.Sum();
        public long Errors { get; } = Summary.EventsBySeverity.GetValueOrDefault(nameof(AuditSeverity.Error)) + Summary.EventsBySeverity.GetValueOrDefault(nameof(AuditSeverity.Critical));
        public long Security { get; } = Summary.EventsByType.GetValueOrDefault(nameof(AuditEventType.Security));
        public long Exceptions { get; } = Summary.EventsByType.GetValueOrDefault(nameof(AuditEventType.Exception));
    }
}
