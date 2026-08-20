using Bunit;
using FSH.BlazorShared.Components;
using FSH.BlazorShared.Models.Chat;
using FSH.BlazorShared.Realtime;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Components;

public sealed class FshChatUnreadBadgeTests : TestSetup
{
    private readonly FakeHub _hub = new();

    public FshChatUnreadBadgeTests()
    {
        Services.AddSingleton<IChatService>(Substitute.For<IChatService>());
        Services.AddSingleton<IHubConnectionService>(_hub);
    }

    [Fact]
    public void Sums_unread_across_channels()
    {
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([
                SampleChannel("General", unread: 0),
                SampleChannel("Random", unread: 3)
            ]);

        var cut = Render<FshChatUnreadBadge>();

        cut.WaitForState(() => cut.Markup.Contains("fsh-chat-unread-badge"), timeout: TimeSpan.FromSeconds(5));
        cut.Find(".fsh-chat-unread-badge").TextContent.ShouldBe("3");
    }

    [Fact]
    public void Clears_when_channel_read_event_arrives()
    {
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([
                SampleChannel("General", unread: 0),
                SampleChannel("Random", unread: 3)
            ]);

        var cut = Render<FshChatUnreadBadge>();
        cut.WaitForState(() => cut.Markup.Contains("fsh-chat-unread-badge"), timeout: TimeSpan.FromSeconds(5));

        // The server pushed ChatChannelRead after MarkChannelRead — the badge
        // must re-fetch and clear instead of waiting for a navigation.
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([
                SampleChannel("General", unread: 0),
                SampleChannel("Random", unread: 0)
            ]);

        _hub.Raise("ChatChannelRead", new { channelId = Guid.NewGuid(), lastReadMessageId = Guid.NewGuid() });

        cut.WaitForState(() => !cut.Markup.Contains("fsh-chat-unread-badge"), timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Recomputes_when_new_message_event_arrives()
    {
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([
                SampleChannel("General", unread: 0),
                SampleChannel("Random", unread: 2)
            ]);

        var cut = Render<FshChatUnreadBadge>();
        cut.WaitForState(() => cut.Find(".fsh-chat-unread-badge").TextContent == "2", timeout: TimeSpan.FromSeconds(5));

        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([
                SampleChannel("General", unread: 0),
                SampleChannel("Random", unread: 5)
            ]);

        _hub.Raise("ChatMessageCreated", new object());

        cut.WaitForState(() => cut.Find(".fsh-chat-unread-badge").TextContent == "5", timeout: TimeSpan.FromSeconds(5));
    }

    private IChatService _chat => Services.GetRequiredService<IChatService>();

    private static ChannelDto SampleChannel(string name, int unread = 0) =>
        new(Guid.NewGuid(), ChannelType.Channel, name, name.ToLowerInvariant(), $"Discussion in {name}",
            false, "user-1", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow.AddHours(-2),
            DateTime.UtcNow.AddMinutes(-30), unread,
            [new ChannelMemberDto(Guid.NewGuid(), "user-1", ChannelMemberRole.Admin, DateTime.UtcNow.AddDays(-7), null)]);

    /// <summary>
    /// Captures hub event handlers by name so tests can raise them on demand —
    /// the real HubConnectionService has no client-to-client channel.
    /// </summary>
    private sealed class FakeHub : IHubConnectionService
    {
        private readonly Dictionary<string, Delegate> _handlers = new(StringComparer.Ordinal);

        public HubConnectionState State { get; set; } = HubConnectionState.Connected;

        public event Action<HubConnectionState>? StateChanged;

        public IDisposable On<T>(string eventName, Func<T, Task> handler)
        {
            _handlers[eventName] = handler;
            return new DisposableStub();
        }

        public void Raise(string eventName, object arg)
        {
            if (_handlers.TryGetValue(eventName, out var handler))
            {
                var invoke = handler.GetType().GetMethod("Invoke")!;
                _ = ((Task)invoke.Invoke(handler, [arg])!);
            }
        }

        public void RaiseStateChanged(HubConnectionState state) => StateChanged?.Invoke(state);

        public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task StopAsync() => Task.CompletedTask;

        public Task SendAsync(string methodName, object? arg, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class DisposableStub : IDisposable
    {
        public void Dispose() { }
    }
}