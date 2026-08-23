using FSH.BlazorShared.Formatting;
using FSH.BlazorShared.Models.Chat;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Realtime;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Chat;

public sealed partial class ChatPage : IAsyncDisposable
{
    private const int InitialPageSize = 100;
    private const int OlderPageSize = 50;
    private static readonly TimeSpan MergeWindow = TimeSpan.FromMinutes(5);

    [Parameter] public Guid Id { get; set; }

    [Inject] private IChatService ChatService { get; set; } = default!;
    [Inject] private IHubConnectionService Hub { get; set; } = default!;
    [Inject] private IUserService Users { get; set; } = default!;
    [Inject] private AuthenticationStateProvider Auth { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private readonly List<ChannelDto> _channels = [];
    private readonly List<MessageDto> _messages = [];
    private readonly HashSet<string> _typingUsers = [];
    private readonly Dictionary<string, UserDto?> _userCache = new(StringComparer.Ordinal);

    private List<ChatRenderItem> _renderItems = [];

    private string TypingNames => string.Join(", ", _typingUsers
        .OrderBy(id => id)
        .Select(id => DisplayNameFor(GetOrResolveUser(id), id)));

    /// <summary>Rail section: public channels, client-side filtered (React parity).</summary>
    private List<ChannelDto> ChannelsSection => _channels
        .Where(c => c.Type == ChannelType.Channel && MatchesFilter(c))
        .ToList();

    /// <summary>Rail section: direct + group messages, client-side filtered (React parity).</summary>
    private List<ChannelDto> DmsSection => _channels
        .Where(c => c.Type is ChannelType.DirectMessage or ChannelType.GroupMessage && MatchesFilter(c))
        .ToList();

    private bool MatchesFilter(ChannelDto channel)
    {
        var query = _searchChannels?.Trim();
        return string.IsNullOrEmpty(query)
            || ChannelTitleFor(channel).Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private string? _searchChannels;
    private Guid? _activeChannelId;
    private ChannelDto? _activeChannel;
    private string? _messageText;
    private MessageDto? _replyToMessage;
    private Guid? _editingMessageId;
    private string _currentUserId = string.Empty;
    private bool _loadingChannels;

    /// <summary>
    /// Mobile single-pane switch (React parity: chat-page.tsx "hidden md:flex").
    /// Below the md breakpoint either the rail or the conversation is visible,
    /// never both stacked; selecting a channel opens the pane, the header back
    /// button returns to the list. Desktop ignores this entirely.
    /// </summary>
    private bool _mobileRailVisible = true;

    private void BackToRailMobile()
    {
        _mobileRailVisible = true;
        _typingUsers.Clear();
    }
    private bool _loadingMessages;
    private bool _loadingOlder;
    private bool _hasOlder = true;
    private double _scrollHeightBeforeOlderLoad = -1;
    private IJSObjectReference? _scrollModule;
    private DotNetObjectReference<ChatPage>? _dotNetRef;
    private ElementReference _messagesContainer;

    private readonly List<IDisposable> _signalrSubscriptions = [];

    protected override async Task OnInitializedAsync()
    {
        await ResolveCurrentUserAsync();
        await LoadChannels();
        await EnsureHubConnected();
        if (_activeChannelId.HasValue) { try { await Hub.SendAsync("JoinChannel", _activeChannelId.Value); } catch {} }
        SubscribeToSignalREvents();
    }

    protected override async Task OnParametersSetAsync()
    {
        await SyncChannelFromRouteAsync();
    }

    /// <summary>
    /// Resolves the signed-in user's id from the "sub" claim (React parity:
    /// the JWT subject). Everything downstream — own-message alignment, reaction
    /// highlighting, typing filters — keys off this value.
    /// </summary>
    private async Task ResolveCurrentUserAsync()
    {
        try
        {
            var state = await Auth.GetAuthenticationStateAsync();
            _currentUserId = state.User.FindFirst("sub")?.Value ?? string.Empty;
            if (!string.IsNullOrEmpty(_currentUserId))
            {
                try
                {
                    var profile = await Users.GetMyProfileAsync();
                    _userCache[_currentUserId] = profile;
                }
                catch
                {
                    // Profile fetch is best-effort; display names still resolve per-message.
                }
            }
        }
        catch
        {
            // Auth state unavailable (prerender/tests) — treat as anonymous.
        }
    }

    private UserDto? GetOrResolveUser(string userId)
        => _userCache.TryGetValue(userId, out var cached) ? cached : null;

    /// <summary>
    /// Fetches any uncached author profiles off the UI thread (fire-and-forget
    /// callers re-render when the names arrive). Never blocks on the Blazor
    /// thread — WASM interop would deadlock on a synchronous wait.
    /// </summary>
    private async Task EnsureUsersResolvedAsync(IEnumerable<string> userIds)
    {
        var missing = userIds
            .Where(id => !string.IsNullOrEmpty(id) && !_userCache.ContainsKey(id))
            .Distinct()
            .ToList();
        if (missing.Count == 0)
        {
            return;
        }

        var added = false;
        foreach (var userId in missing)
        {
            try
            {
                _userCache[userId] = await Users.GetAsync(userId);
                added = true;
            }
            catch
            {
                _userCache[userId] = null;
            }
        }

        if (added)
        {
            RebuildRenderItems();
            await InvokeAsync(StateHasChanged);
        }
    }

    private static string DisplayNameFor(UserDto? user, string userId)
    {
        if (user is not null)
        {
            var name = string.Join(' ', new[] { user.FirstName, user.LastName }).Trim();
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            if (!string.IsNullOrEmpty(user.UserName))
            {
                return user.UserName;
            }

            if (!string.IsNullOrEmpty(user.Email))
            {
                return user.Email;
            }
        }

        return ShortId(userId);
    }

    private static string HandleFor(UserDto? user, string userId)
    {
        var handle = user?.UserName ?? user?.Email ?? ShortId(userId);
        return $"@{handle}";
    }

    private static string ShortId(string userId)
        => userId.Length > 8 ? userId[..8] : userId;

    private static string TimeFor(DateTime utc) => utc.ToLocalTime().ToString("h:mm tt");

    private string? GetAvatarUrl(string userId) => _userCache.TryGetValue(userId, out var u) ? u?.ImageUrl : null;
    private string InitialsFor(UserDto? user, string userId)
    {
        var name = DisplayNameFor(user, userId);
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
            : (name.Length > 0 ? name[..Math.Min(2, name.Length)].ToUpperInvariant() : "?");
    }

    /// <summary>
    /// Rebuilds the flattened render list (day separators + message blocks).
    /// Consecutive messages from the same author within <see cref="MergeWindow"/>
    /// collapse into one block so the avatar/name header appears once (React
    /// parity: chat message merge window is 5 minutes).
    /// </summary>
    private void RebuildRenderItems()
    {
        var items = new List<ChatRenderItem>(_messages.Count);
        MessageBlockItem? currentBlock = null;
        DateTime? lastDate = null;

        foreach (var msg in _messages)
        {
            var localDate = msg.CreatedAtUtc.ToLocalTime().Date;
            if (lastDate is null || localDate != lastDate)
            {
                items.Add(new DaySeparatorItem(DayLabel(localDate)));
                lastDate = localDate;
            }

            var canMerge = !msg.DeletedAtUtc.HasValue
                && currentBlock is not null
                && currentBlock.AuthorId == msg.AuthorUserId
                && currentBlock.Messages[^1].CreatedAtUtc >= msg.CreatedAtUtc - MergeWindow
                && msg.ParentMessageId == currentBlock.ParentMessageId;

            if (canMerge && currentBlock is not null)
            {
                currentBlock.Messages.Add(msg);
            }
            else
            {
                var author = GetOrResolveUser(msg.AuthorUserId);
                currentBlock = new MessageBlockItem(
                    msg.AuthorUserId,
                    msg.AuthorUserId == _currentUserId,
                    author,
                    DisplayNameFor(author, msg.AuthorUserId),
                    HandleFor(author, msg.AuthorUserId),
                    msg.ParentMessageId,
                    [msg]);
                items.Add(currentBlock);
            }
        }

        _renderItems = items;
    }

    private static string DayLabel(DateTime date)
    {
        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);
        return date.Date == today ? "Today"
            : date.Date == yesterday ? "Yesterday"
            : date.Year == today.Year ? date.ToString("MMMM d")
            : date.ToString("MMMM d, yyyy");
    }

    /// <summary>Mirrors the React chat route (/chat[:channelId]): a channel id in the URL opens</summary>
    private async Task SyncChannelFromRouteAsync()
    {
        if (_loadingChannels || _channels.Count == 0)
        {
            return;
        }

        if (Id == Guid.Empty)
        {
            // No id in the route → select the first channel and replace the URL (deep-link parity).
            if (_activeChannelId is null)
            {
                Nav.NavigateTo($"/chat/{_channels[0].Id}", replace: true);
                await SelectChannel(_channels[0].Id);
            }

            return;
        }

        if (_activeChannelId != Id && _channels.Any(c => c.Id == Id))
        {
            await SelectChannel(Id);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await AttachScrollWatcherAsync();
        }
        else if (_scrollHeightBeforeOlderLoad >= 0)
        {
            // Older messages were just prepended — restore the previous scroll offset
            // so the user's reading position doesn't jump.
            var fromHeight = _scrollHeightBeforeOlderLoad;
            _scrollHeightBeforeOlderLoad = -1;
            try
            {
                if (_scrollModule is not null)
                {
                    await _scrollModule.InvokeVoidAsync("restoreScrollPosition", fromHeight);
                }
            }
            catch
            {
                // Interop unavailable (prerender/tests) — skip restoration.
            }
        }
    }

    private async Task AttachScrollWatcherAsync()
    {
        try
        {
            _scrollModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/FSH.BlazorShared/js/fshChatScroll.js");
            _dotNetRef ??= DotNetObjectReference.Create(this);
            await _scrollModule.InvokeVoidAsync("watchScrollTop", _dotNetRef);
        }
        catch
        {
            // Interop unavailable (prerender/tests) — infinite scroll simply won't trigger.
        }
    }

    [JSInvokable]
    public async Task OnScrollTopReached()
    {
        await LoadOlderMessagesAsync();
    }

    private async Task LoadOlderMessagesAsync()
    {
        if (_activeChannelId is null || _loadingOlder || !_hasOlder || _messages.Count == 0)
        {
            return;
        }

        _loadingOlder = true;
        try
        {
            _scrollHeightBeforeOlderLoad = _scrollModule is null
                ? 0
                : await _scrollModule.InvokeAsync<double>("scrollHeight");
            var older = await ChatService.ListChannelMessagesAsync(_activeChannelId.Value, before: _messages[0].Id, pageSize: OlderPageSize);
            if (older.Count == 0)
            {
                _hasOlder = false;
            }
            else
            {
                // Prepend older history (ascending), dedupe against anything that
                // arrived via SignalR while the fetch was in flight.
                var existing = _messages.Select(m => m.Id).ToHashSet();
                var toAdd = older.Where(m => existing.Add(m.Id)).OrderBy(m => m.CreatedAtUtc).ToList();
                _messages.InsertRange(0, toAdd);
                if (older.Count < OlderPageSize)
                {
                    _hasOlder = false;
                }
            }

            RebuildRenderItems();
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            _scrollHeightBeforeOlderLoad = -1;
            Snackbar.Add($"Failed to load earlier messages: {ex.Message}", Severity.Warning);
        }
        finally
        {
            _loadingOlder = false;
        }
    }

    private async Task EnsureHubConnected()
    {
        if (Hub.State != HubConnectionState.Connected)
        {
            await Hub.StartAsync();
        }
        JoinAllChannels();
    }

    private void JoinAllChannels()
    {
        if (Hub.State != HubConnectionState.Connected) return;
        foreach (var ch in _channels)
        {
            try { _ = Hub.SendAsync("JoinChannel", ch.Id); } catch { /* best effort */ }
        }
    }

    private void SubscribeToSignalREvents()
    {
        _signalrSubscriptions.Add(Hub.On<MessageDto>("ChatMessageCreated", async msg =>
        {
            // Resolve author profile immediately
            _ = EnsureUsersResolvedAsync([msg.AuthorUserId]);

            if (msg.ChannelId == _activeChannelId)
            {
                // Active channel: add message, scroll, mark read
                if (!_messages.Any(m => m.Id == msg.Id))
                {
                    _messages.Add(msg);
                    RebuildRenderItems();
                    await InvokeAsync(StateHasChanged);
                    await ScrollToBottom();
                }
                await MarkActiveChannelReadAsync();
            }
            else
            {
                // Non-active channel: show notification snackbar (React parity: toast).
                // A brand-new DM created by the peer may not be in our rail yet —
                // refetch (mirrors React's invalidateQueries(["chat","my-channels"]))
                // so the toast carries the real title and clicking through works.
                var ch = _channels.FirstOrDefault(c => c.Id == msg.ChannelId);
                if (ch is null)
                {
                    await LoadChannels();
                    ch = _channels.FirstOrDefault(c => c.Id == msg.ChannelId);
                }

                var authorName = DisplayNameFor(GetOrResolveUser(msg.AuthorUserId), msg.AuthorUserId);
                var preview = msg.Body?.Length > 60 ? msg.Body[..60] + "…" : msg.Body ?? "";
                var where = ch is null ? "a conversation"
                    : ch.Type == ChannelType.Channel ? $"#{ch.Name}"
                    : ChannelTitleFor(ch);
                // React parity: sonner toast with close affordance, not clickable.
                Snackbar.Add($"{authorName} in {where}: {preview}", Severity.Info, config =>
                {
                    config.ShowCloseIcon = true;
                });

                // Update unread count + recency on channel list
                if (ch is not null)
                {
                    var idx = _channels.IndexOf(ch);
                    _channels[idx] = ch with { UnreadCount = ch.UnreadCount + 1, LastMessageAtUtc = msg.CreatedAtUtc };
                    await InvokeAsync(StateHasChanged);
                }
            }
        }));

        _signalrSubscriptions.Add(Hub.On<object>("ChatMessageEdited", async _ =>
        {
            if (_activeChannelId is not null)
            {
                await LoadMessages(_activeChannelId.Value);
            }
        }));

        _signalrSubscriptions.Add(Hub.On<object>("ChatMessageDeleted", async _ =>
        {
            if (_activeChannelId is not null)
            {
                await LoadMessages(_activeChannelId.Value);
            }
        }));

        _signalrSubscriptions.Add(Hub.On<object>("ChatReactionChanged", async _ =>
        {
            if (_activeChannelId is not null)
            {
                await LoadMessages(_activeChannelId.Value);
            }
        }));

        _signalrSubscriptions.Add(Hub.On<object>("ChatChannelAdded", async _ =>
        {
            await LoadChannels();
            await InvokeAsync(StateHasChanged);
        }));

        _signalrSubscriptions.Add(Hub.On<TypingEvent>("ChatTypingStarted", async evt =>
        {
            if (evt.ChannelId == _activeChannelId && evt.UserId != _currentUserId)
            {
                _typingUsers.Add(evt.UserId);
                await InvokeAsync(StateHasChanged);
                _ = EnsureUsersResolvedAsync([evt.UserId]);

                // Auto-remove after 3s
                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    _typingUsers.Remove(evt.UserId);
                    await InvokeAsync(StateHasChanged);
                });
            }
        }));
    }

    private async Task LoadChannels()
    {
        _loadingChannels = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var channels = await ChatService.ListMyChannelsAsync(ct: CancellationToken.None);
            _channels.Clear();
            _channels.AddRange(channels.OrderByDescending(c => c.LastMessageAtUtc ?? c.CreatedAtUtc));
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load channels: {ex.Message}", Severity.Error);
        }

        _loadingChannels = false;
        await InvokeAsync(StateHasChanged);
        UpdatePresence();
        foreach(var ch in _channels) try { await Hub.SendAsync("JoinChannel", ch.Id); } catch {}

        // Fire-and-forget: resolve DM/group partner profiles so rail + header
        // titles show real names once the user cache fills (React parity).
        _ = EnsureUsersResolvedAsync(_channels
            .Where(c => c.Type is ChannelType.DirectMessage or ChannelType.GroupMessage)
            .SelectMany(c => c.Members)
            .Select(m => m.UserId));
    }

    private async Task SelectChannel(Guid channelId)
    {
        _activeChannelId = channelId;
        _activeChannel = _channels.FirstOrDefault(c => c.Id == channelId);
        _typingUsers.Clear();
        _mobileRailVisible = false;

        // Join the SignalR group for live messages (React parity: JoinChannel on select)
        if (Hub.State == HubConnectionState.Connected)
        {
            try { await Hub.SendAsync("JoinChannel", channelId); } catch { /* best effort */ }
        }

        await LoadMessages(channelId);
        Nav.NavigateTo($"/chat/{channelId}");
        await MarkActiveChannelReadAsync();
    }

    /// <summary>
    /// Advances the server-side read watermark for the active channel to its
    /// latest loaded message, then zeroes the local unread count. Called when
    /// a channel is opened and whenever a new message lands in the active
    /// channel (React parity: chat-page's mark-read effect fires on every
    /// latest-message change). Best-effort — never blocks the UI.
    /// </summary>
    private async Task MarkActiveChannelReadAsync()
    {
        if (_activeChannelId is null)
        {
            return;
        }

        var lastMsg = _messages.LastOrDefault();
        if (lastMsg is null)
        {
            return;
        }

        try
        {
            await ChatService.MarkChannelReadAsync(_activeChannelId.Value, new MarkChannelReadRequest(lastMsg.Id));

            var idx = _channels.FindIndex(c => c.Id == _activeChannelId.Value);
            if (idx >= 0 && _channels[idx].UnreadCount > 0)
            {
                _channels[idx] = _channels[idx] with { UnreadCount = 0 };
                await InvokeAsync(StateHasChanged);
            }
        }
        catch
        {
            // Best effort — the watermark is housekeeping, never block the UI.
        }
    }

    private async Task LoadMessages(Guid channelId)
    {
        _loadingMessages = true;
        _hasOlder = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var messages = await ChatService.ListChannelMessagesAsync(channelId, pageSize: InitialPageSize);
            _messages.Clear();
            _messages.AddRange(messages.OrderBy(m => m.CreatedAtUtc));
            _hasOlder = messages.Count >= InitialPageSize;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load messages: {ex.Message}", Severity.Error);
        }

        RebuildRenderItems();
        UpdatePresence();
        _loadingMessages = false;
        await InvokeAsync(StateHasChanged);
        _ = EnsureUsersResolvedAsync(_messages.Select(m => m.AuthorUserId));
        await ScrollToBottom();
    }

    private async Task SendMessage()
    {
        if (_activeChannelId is null || string.IsNullOrWhiteSpace(_messageText)) return;

        var text = _messageText!.Trim();
        if (string.IsNullOrEmpty(text)) return;
        var replyId = _replyToMessage?.Id;
        var editingId = _editingMessageId;
        _messageText = string.Empty;
        var prevReply = _replyToMessage;
        var prevEdit = _editingMessageId;
        _replyToMessage = null;
        _editingMessageId = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            if (editingId.HasValue)
            {
                await ChatService.EditMessageAsync(editingId.Value, new EditMessageRequest(text));
                if (_activeChannelId.HasValue) await LoadMessages(_activeChannelId.Value);
            }
            else
            {
                var sent = await ChatService.SendMessageAsync(_activeChannelId.Value, new SendMessageRequest(Body: text, ParentMessageId: replyId));
                if (!_messages.Any(m => m.Id == sent.Id))
                {
                    _messages.Add(sent);
                    RebuildRenderItems();
                }
                await ScrollToBottom();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to send message: {ex.Message}", Severity.Error);
            _messageText = text;
            _replyToMessage = prevReply;
            _editingMessageId = prevEdit;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await SendMessage();
        }
    }

    private DateTime _lastTypingSentAt = DateTime.MinValue;

    private async Task OnMessageTextChanged(string value)
    {
        _messageText = value;
        // Throttle typing indicator to at most once per 2 seconds
        if (_activeChannelId is null || string.IsNullOrWhiteSpace(value)) return;
        if (DateTime.UtcNow - _lastTypingSentAt < TimeSpan.FromSeconds(2)) return;
        _lastTypingSentAt = DateTime.UtcNow;
        try
        {
            await Hub.SendAsync("Typing", _activeChannelId.Value);
        }
        catch
        {
            // Best effort
        }
    }

    private async Task SendTypingIndicator()
    {
        if (_activeChannelId is null) return;
        try
        {
            await Hub.SendAsync("Typing", _activeChannelId.Value);
        }
        catch
        {
            // Best effort
        }
    }

    private async Task ToggleReaction(Guid messageId, string emoji)
    {
        try
        {
            var msg = _messages.FirstOrDefault(m => m.Id == messageId);
            if (msg is null) return;

            var existing = msg.Reactions.FirstOrDefault(r => r.Emoji == emoji && r.UserId == _currentUserId);
            if (existing is not null)
            {
                await ChatService.RemoveReactionAsync(messageId, emoji);
            }
            else
            {
                await ChatService.AddReactionAsync(messageId, new AddReactionRequest(emoji));
            }

            // Refresh messages to get updated reactions
            if (_activeChannelId is not null)
            {
                await LoadMessages(_activeChannelId.Value);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to update reaction: {ex.Message}", Severity.Warning);
        }
    }

    private bool IsUserOnline(string userId) => _onlineUsers.Contains(userId);
    private readonly HashSet<string> _onlineUsers = new(StringComparer.Ordinal);
    private void UpdatePresence(){ _onlineUsers.Clear(); foreach(var ch in _channels) foreach(var m in ch.Members) _onlineUsers.Add(m.UserId); foreach(var msg in _messages) _onlineUsers.Add(msg.AuthorUserId); }
    private void BeginReply(MessageDto msg)
    {
        _replyToMessage = msg;
        _editingMessageId = null;
    }

    private void BeginEdit(MessageDto msg)
    {
        _editingMessageId = msg.Id;
        _messageText = msg.Body ?? string.Empty;
        _replyToMessage = null;
    }

    private void CancelReplyEdit()
    {
        var wasEditing = _editingMessageId.HasValue;
        _replyToMessage = null;
        _editingMessageId = null;
        if (wasEditing) _messageText = string.Empty;
    }

    private async Task TogglePin(MessageDto msg)
    {
        try
        {
            if (msg.IsPinned) await ChatService.UnpinMessageAsync(msg.Id);
            else await ChatService.PinMessageAsync(msg.Id);
            if (_activeChannelId.HasValue) await LoadMessages(_activeChannelId.Value);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Pin failed: {ex.Message}", Severity.Warning);
        }
    }

    private async Task DeleteMessage(MessageDto msg)
    {
        var parameters = new DialogParameters { { "Message", "Delete this message? This cannot be undone." }, { "ConfirmText", "Delete" }, { "CancelText", "Cancel" } };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };
        var dialog = await DialogService.ShowAsync<FSH.BlazorShared.Components.FshConfirmDialogContent>("Delete message", parameters, options);
        var result = await dialog.Result;
        if (result is null || result.Canceled) return;
        try
        {
            await ChatService.DeleteMessageAsync(msg.Id);
            if (_activeChannelId.HasValue) await LoadMessages(_activeChannelId.Value);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Delete failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task ScrollToMessage(Guid messageId)
    {
        try
        {
            await JS.InvokeVoidAsync("eval", $"document.querySelector('[data-message-id=\"{messageId}\"]')?.scrollIntoView({{behavior:'smooth',block:'center'}})");
        }
        catch { }
    }

    private async Task OpenSearchDialog()
    {
        var parameters = new DialogParameters<ChatSearchDialog>();
        parameters.Add(nameof(ChatSearchDialog.Messages), _messages);
        parameters.Add(nameof(ChatSearchDialog.OnJumpToMessage), EventCallback.Factory.Create<Guid>(this, async id => await ScrollToMessage(id)));
        await DialogService.ShowAsync<ChatSearchDialog>("Search Messages", parameters, new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true });
    }

    private async Task OpenChannelSettingsDialog()
    {
        if (_activeChannel is null) return;
        var parameters = new DialogParameters<ChannelSettingsDialog>();
        parameters.Add(nameof(ChannelSettingsDialog.Channel), _activeChannel);
        parameters.Add(nameof(ChannelSettingsDialog.OnChannelUpdated), EventCallback.Factory.Create(this, async () =>
        {
            await LoadChannels();
            if (_activeChannelId.HasValue) await LoadMessages(_activeChannelId.Value);
        }));
        await DialogService.ShowAsync<ChannelSettingsDialog>("Channel Settings", parameters, new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true });
    }

    private void ShowReplies(Guid messageId)
    {
        Snackbar.Add("Threaded replies — full view coming soon", Severity.Info);
    }

    private async Task OpenCreateChannelDialog()
    {
        // Will be implemented as a separate dialog component
        var parameters = new DialogParameters<CreateChannelDialog>();
        parameters.Add(nameof(CreateChannelDialog.OnCreated), EventCallback.Factory.Create(this, async () =>
        {
            await LoadChannels();
            await InvokeAsync(StateHasChanged);
        }));
        await DialogService.ShowAsync<CreateChannelDialog>("Create Channel", parameters);
    }

    private async Task OpenNewDmDialog()
    {
        var parameters = new DialogParameters<NewDmDialog>
        {
            { nameof(NewDmDialog.SelfUserId), _currentUserId }
        };
        parameters.Add(nameof(NewDmDialog.OnCreated), EventCallback.Factory.Create<Guid>(this, async channelId =>
        {
            await LoadChannels();
            await SelectChannel(channelId);
        }));
        await DialogService.ShowAsync<NewDmDialog>("New Direct Message", parameters);
    }

    /// <summary>
    /// Rail/header display title, mirroring the React app's channelTitle
    /// resolution: channels use their name, DMs use the partner's display name,
    /// group messages join up to three other member names ("+N" for the rest).
    /// </summary>
    private string ChannelTitleFor(ChannelDto channel)
    {
        if (channel.Type == ChannelType.Channel)
        {
            return string.IsNullOrWhiteSpace(channel.Name) ? "(unnamed channel)" : channel.Name;
        }

        var others = channel.Members
            .Select(m => m.UserId)
            .Where(id => !string.IsNullOrEmpty(id) && id != _currentUserId)
            .ToList();

        if (channel.Type == ChannelType.DirectMessage)
        {
            var partnerId = others.FirstOrDefault();
            return string.IsNullOrEmpty(partnerId)
                ? "Direct message"
                : DisplayNameFor(GetOrResolveUser(partnerId), partnerId);
        }

        var names = others
            .Take(3)
            .Select(id => DisplayNameFor(GetOrResolveUser(id), id))
            .ToList();
        var extra = others.Count - names.Count;
        return names.Count == 0
            ? "Group message"
            : extra > 0
                ? $"{string.Join(", ", names)} +{extra}"
                : string.Join(", ", names);
    }

    private async Task ScrollToBottom()
    {
        try
        {
            if (_scrollModule is not null)
            {
                await _scrollModule.InvokeVoidAsync("scrollToBottom");
            }
        }
        catch
        {
            // JS interop may fail during prerender
        }
    }

    private static string ChannelIcon(ChannelType type) => type switch
    {
        ChannelType.DirectMessage => Icons.Material.Outlined.Person,
        ChannelType.GroupMessage => Icons.Material.Outlined.Group,
        _ => Icons.Material.Outlined.Tag,
    };

    private static string ChannelItemClass(bool isActive) =>
        $"fsh-chat-channel-item {(isActive ? "fsh-chat-channel-active" : "")}";

    private static Color ChannelIconColor(bool isActive) => isActive ? Color.Primary : Color.Default;

    private static string ChannelTextWeight(bool isActive) =>
        $"font-weight: {(isActive ? "600" : "400")}; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;";

    private static string ReactionClass(bool isMine) => isMine ? "fsh-chat-reaction mine" : "fsh-chat-reaction";

    public async ValueTask DisposeAsync()
    {
        foreach (var sub in _signalrSubscriptions)
        {
            sub.Dispose();
        }

        try
        {
            if (_scrollModule is not null && _dotNetRef is not null)
            {
                await _scrollModule.InvokeVoidAsync("unwatchScrollTop", _dotNetRef);
                _dotNetRef.Dispose();
                await _scrollModule.DisposeAsync();
            }
        }
        catch
        {
            // Interop may already be torn down during navigation.
        }
    }

    private sealed record TypingEvent(Guid ChannelId, string UserId);

    private abstract record ChatRenderItem;

    private sealed record DaySeparatorItem(string Label) : ChatRenderItem;

    private sealed record MessageBlockItem(
        string AuthorId,
        bool IsOwn,
        UserDto? Author,
        string DisplayName,
        string Handle,
        Guid? ParentMessageId,
        List<MessageDto> Messages) : ChatRenderItem;
}