using FSH.BlazorShared.Models.Chat;
using FSH.BlazorShared.Realtime;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Chat;

public sealed partial class ChatPage : IAsyncDisposable
{
    [Inject] private IChatService ChatService { get; set; } = default!;
    [Inject] private IHubConnectionService Hub { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private readonly List<ChannelDto> _channels = [];
    private readonly List<MessageDto> _messages = [];
    private readonly HashSet<string> _typingUsers = [];

    private string? _searchChannels;
    private Guid? _activeChannelId;
    private ChannelDto? _activeChannel;
    private string? _messageText;
    private string _currentUserId = string.Empty;
    private bool _loadingChannels;
    private bool _loadingMessages;
    private ElementReference _messagesContainer;

    private readonly List<IDisposable> _signalrSubscriptions = [];
    private Timer? _typingThrottle;

    protected override async Task OnInitializedAsync()
    {
        await LoadChannels();
        await EnsureHubConnected();
        SubscribeToSignalREvents();
    }

    private async Task EnsureHubConnected()
    {
        if (Hub.State != HubConnectionState.Connected)
        {
            await Hub.StartAsync();
        }
    }

    private void SubscribeToSignalREvents()
    {
        _signalrSubscriptions.Add(Hub.On<MessageDto>("ChatMessageCreated", async msg =>
        {
            if (msg.ChannelId == _activeChannelId)
            {
                _messages.Add(msg);
                await InvokeAsync(StateHasChanged);
                await ScrollToBottom();
            }
            else
            {
                // Update unread count on channel list
                var ch = _channels.FirstOrDefault(c => c.Id == msg.ChannelId);
                if (ch is not null)
                {
                    ch = ch with { UnreadCount = ch.UnreadCount + 1, LastMessageAtUtc = msg.CreatedAtUtc };
                    var idx = _channels.IndexOf(_channels.First(c => c.Id == msg.ChannelId));
                    _channels[idx] = ch;
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
    }

    private async Task SelectChannel(Guid channelId)
    {
        _activeChannelId = channelId;
        _activeChannel = _channels.FirstOrDefault(c => c.Id == channelId);
        _typingUsers.Clear();

        // Mark as read
        if (_activeChannel?.UnreadCount > 0)
        {
            try
            {
                var lastMsg = _messages.LastOrDefault();
                if (lastMsg is not null)
                {
                    await ChatService.MarkChannelReadAsync(channelId, new MarkChannelReadRequest(lastMsg.Id));
                }

                var idx = _channels.IndexOf(_channels.First(c => c.Id == channelId));
                _channels[idx] = _channels[idx] with { UnreadCount = 0 };
            }
            catch
            {
                // Best effort — don't block UI
            }
        }

        await LoadMessages(channelId);
    }

    private async Task LoadMessages(Guid channelId)
    {
        _loadingMessages = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var messages = await ChatService.ListChannelMessagesAsync(channelId, pageSize: 100);
            _messages.Clear();
            _messages.AddRange(messages.OrderBy(m => m.CreatedAtUtc));
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load messages: {ex.Message}", Severity.Error);
        }

        _loadingMessages = false;
        await InvokeAsync(StateHasChanged);
        await ScrollToBottom();
    }

    private async Task SendMessage()
    {
        if (_activeChannelId is null || string.IsNullOrWhiteSpace(_messageText)) return;

        var text = _messageText;
        _messageText = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            var sent = await ChatService.SendMessageAsync(_activeChannelId.Value, new SendMessageRequest(Body: text));
            // Message will arrive via SignalR; also add optimistically
            if (!_messages.Any(m => m.Id == sent.Id))
            {
                _messages.Add(sent);
            }
            await ScrollToBottom();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to send message: {ex.Message}", Severity.Error);
            _messageText = text; // Restore on failure
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await SendMessage();
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

    private async Task ScrollToBottom()
    {
        try
        {
            await JS.InvokeVoidAsync("eval", "document.querySelector('.fsh-chat-messages')?.scrollTo(0, 999999)");
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

    public async ValueTask DisposeAsync()
    {
        foreach (var sub in _signalrSubscriptions)
        {
            sub.Dispose();
        }
        _typingThrottle?.Dispose();
    }

    private sealed record TypingEvent(Guid ChannelId, string UserId);

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
}
