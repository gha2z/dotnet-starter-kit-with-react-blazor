using FSH.BlazorShared.Models.Tickets;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Tickets;

public sealed partial class TicketsListPage : IDisposable
{
    [Inject] private ITicketService Tickets { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private const int PageSize = 20;

    private List<TicketDto> _items = new();
    private int _pageNumber = 1;
    private int _totalCount;
    private int _totalPages = 1;
    private string _search = string.Empty;
    private TicketStatus? _statusFilter;
    private TicketPriority? _priorityFilter;
    private bool _loading = true;
    private string? _error;
    private bool _createDialogOpen;
    private CancellationTokenSource? _debounceCts;

    private static readonly TicketStatus?[] AllStatuses = [null, TicketStatus.Open, TicketStatus.InProgress, TicketStatus.Resolved, TicketStatus.Closed];
    private static readonly TicketPriority?[] AllPriorities = [null, TicketPriority.Low, TicketPriority.Medium, TicketPriority.High, TicketPriority.Critical];

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            var page = await Tickets.SearchTicketsAsync(
                search: SearchActive ? _search.Trim() : null,
                status: _statusFilter,
                priority: _priorityFilter,
                pageNumber: _pageNumber,
                pageSize: PageSize,
                sortBy: "createdAtUtc",
                sortDir: "desc");
            _items = page.Items.ToList();
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
    private bool HasFilters => SearchActive || _statusFilter.HasValue || _priorityFilter.HasValue;

    // Awaited (not fire-and-forget): the completing event handler triggers the re-render.
    // A `_ = LoadAsync()` here left the page stuck on the loading skeletons forever —
    // the fetch completed but nothing told the renderer (user-reported filter bug).
    private async Task SetStatus(TicketStatus? s) { _statusFilter = s; _pageNumber = 1; await LoadAsync(); }
    private async Task SetPriority(TicketPriority? p) { _priorityFilter = p; _pageNumber = 1; await LoadAsync(); }

    /// <summary>
    /// Debounced server-side search: cancels any in-flight debounce on a new
    /// keystroke and reloads 250 ms after typing settles (mirrors
    /// UsersListPage). Without this, Immediate="true" only updated the bound
    /// field and never re-queried the backend.
    /// </summary>
    private async Task OnSearchChangedAsync(string value)
    {
        _search = value;
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        try
        {
            await Task.Delay(250, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

        _pageNumber = 1;
        await LoadAsync();
    }

    public void Dispose() => _debounceCts?.Dispose();

    private async Task GoToPage(int page) { _pageNumber = Math.Clamp(page, 1, Math.Max(_totalPages, 1)); await LoadAsync(); }

    private async Task ClearFilters()
    {
        _search = string.Empty;
        _statusFilter = null;
        _priorityFilter = null;
        _pageNumber = 1;
        await LoadAsync();
    }

    private void OpenCreateDialog() => _createDialogOpen = true;

    private void GoToTicket(Guid id) => Navigation.NavigateTo($"/tickets/{id}");

    private async Task OnTicketCreated()
    {
        _createDialogOpen = false;
        await LoadAsync();
    }

    private string EmptyDescription => HasFilters
        ? SearchActive
            ? $"Nothing matches \"{_search.Trim()}\". Try a different term or clear the search."
            : "No tickets match the current filters."
        : "Open the first ticket to start tracking work. Tickets carry a status, priority, an optional assignee, and a comment thread.";

    private string MetaText =>
        $"{_items.Count} of {_totalCount} ticket{(_totalCount == 1 ? "" : "s")}"
        + (_totalPages > 1 ? $" · page {_pageNumber} of {_totalPages}" : "");

    // ─── Labels + tones ────────────────────────────────────────────────

    private static string StatusLabel(TicketStatus? s) => s switch
    {
        TicketStatus.Open => "Open",
        TicketStatus.InProgress => "In progress",
        TicketStatus.Resolved => "Resolved",
        TicketStatus.Closed => "Closed",
        _ => "All"
    };

    private static string PriorityLabel(TicketPriority? p) => p switch
    {
        TicketPriority.Low => "Low",
        TicketPriority.Medium => "Medium",
        TicketPriority.High => "High",
        TicketPriority.Critical => "Critical",
        _ => "Any"
    };

    private static Color StatusTone(TicketStatus s) => s switch
    {
        TicketStatus.Open => Color.Info,
        TicketStatus.InProgress => Color.Warning,
        TicketStatus.Resolved => Color.Success,
        TicketStatus.Closed => Color.Default,
        _ => Color.Default
    };

    private static Color PriorityTone(TicketPriority p) => p switch
    {
        TicketPriority.Critical => Color.Error,
        TicketPriority.High => Color.Warning,
        TicketPriority.Medium => Color.Info,
        TicketPriority.Low => Color.Default,
        _ => Color.Default
    };
}
