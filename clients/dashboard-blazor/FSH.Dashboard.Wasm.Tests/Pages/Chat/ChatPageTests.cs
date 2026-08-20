using Bunit;
using Bunit.TestDoubles;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Chat;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Realtime;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Chat;

public sealed class ChatPageTests : TestSetup
{
    private readonly IChatService _chat = Substitute.For<IChatService>();
    private readonly IHubConnectionService _hub = Substitute.For<IHubConnectionService>();
    private readonly IUserService _users = Substitute.For<IUserService>();

    public ChatPageTests()
    {
        _hub.State.Returns(HubConnectionState.Connected);
        _hub.On(Arg.Any<string>(), Arg.Any<Func<object, Task>>())
            .Returns(new DisposableStub());
        Services.AddSingleton(_chat);
        Services.AddSingleton(_hub);
        Services.AddSingleton(_users);
    }

    private static ChannelDto SampleChannel(
        string name = "General",
        ChannelType type = ChannelType.Channel,
        int unread = 0,
        DateTime? lastMessageAt = null) =>
        new(Guid.NewGuid(), type, name, name.ToLowerInvariant(), $"Discussion in {name}",
            false, "user-1", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow.AddHours(-2),
            lastMessageAt ?? DateTime.UtcNow.AddMinutes(-30), unread,
            [new ChannelMemberDto(Guid.NewGuid(), "user-1", ChannelMemberRole.Admin, DateTime.UtcNow.AddDays(-7), null)]);

    private static MessageDto SampleMessage(string body = "Hello world", string author = "user-1", DateTime? createdAt = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), author, body, null, 0, null, null,
            createdAt ?? DateTime.UtcNow.AddMinutes(-5), [], []);

    [Fact]
    public void Renders_channels_and_auto_selects_first_channel()
    {
        var first = SampleChannel("General", lastMessageAt: DateTime.UtcNow.AddHours(-1));
        var second = SampleChannel("Random", ChannelType.Channel, 3, lastMessageAt: DateTime.UtcNow.AddHours(-2));
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([first, second]);
        _chat.ListChannelMessagesAsync(first.Id, Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();

        cut.FindAll(".fsh-chat-channel-item").Count.ShouldBe(2);
        cut.FindAll(".fsh-chat-channel-active").Count.ShouldBe(1);
        Services.GetRequiredService<BunitNavigationManager>().Uri.ShouldEndWith($"/chat/{first.Id}");
    }

    [Fact]
    public void Displays_channels_with_unread_badge()
    {
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([
                SampleChannel("General", lastMessageAt: DateTime.UtcNow.AddHours(-1)),
                SampleChannel("Random", ChannelType.Channel, 5, lastMessageAt: DateTime.UtcNow.AddHours(-2))
            ]);
        _chat.ListChannelMessagesAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();

        var badges = cut.FindAll(".mud-badge");
        badges.Count.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Shows_empty_channel_message_when_no_channels()
    {
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();

        cut.FindAll(".fsh-chat-channel-item").Count.ShouldBe(0);
        cut.Markup.ShouldContain("No channels yet");
    }

    [Fact]
    public void Shows_placeholder_when_no_channels_to_auto_select()
    {
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();

        cut.Markup.ShouldContain("Select a channel to start chatting");
    }

    [Fact]
    public void Selecting_channel_loads_messages()
    {
        var channel = SampleChannel("General");
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([channel]);
        _chat.ListChannelMessagesAsync(channel.Id, Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([SampleMessage("Hello"), SampleMessage("World", "user-2")]);
        _chat.MarkChannelReadAsync(Arg.Any<Guid>(), Arg.Any<MarkChannelReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();

        cut.Find(".fsh-chat-channel-item").Click();

        cut.WaitForState(() => cut.FindAll(".fsh-chat-message").Count == 2, timeout: TimeSpan.FromSeconds(5));
        cut.FindAll(".fsh-chat-message").Count.ShouldBe(2);
    }

    [Fact]
    public void Deep_link_with_channel_id_opens_that_channel()
    {
        var first = SampleChannel("General");
        var second = SampleChannel("Random", ChannelType.Channel, 3);
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([first, second]);
        _chat.ListChannelMessagesAsync(second.Id, Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([SampleMessage("Deep linked")]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>(parameters => parameters.Add(p => p.Id, second.Id));

        cut.WaitForState(() => cut.FindAll(".fsh-chat-message").Count == 1, timeout: TimeSpan.FromSeconds(5));
        var active = cut.Find(".fsh-chat-channel-active");
        active.TextContent.ShouldContain("Random");
        Services.GetRequiredService<BunitNavigationManager>().Uri.ShouldEndWith($"/chat/{second.Id}");
    }

    [Fact]
    public void Deep_link_with_unknown_channel_id_keeps_placeholder()
    {
        var channel = SampleChannel("General");
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([channel]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>(parameters => parameters.Add(p => p.Id, Guid.NewGuid()));

        cut.Markup.ShouldContain("Select a channel to start chatting");
        cut.FindAll(".fsh-chat-channel-active").Count.ShouldBe(0);
    }

    [Fact]
    public void Clicking_channel_navigates_to_chat_url()
    {
        var first = SampleChannel("General", lastMessageAt: DateTime.UtcNow.AddHours(-1));
        var second = SampleChannel("Random", ChannelType.Channel, 3, lastMessageAt: DateTime.UtcNow.AddHours(-2));
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([first, second]);
        _chat.ListChannelMessagesAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();

        cut.FindAll(".fsh-chat-channel-item")[1].Click();
        cut.WaitForState(() => cut.FindAll(".fsh-chat-channel-active").Count == 1, timeout: TimeSpan.FromSeconds(5));

        Services.GetRequiredService<BunitNavigationManager>().Uri.ShouldEndWith($"/chat/{second.Id}");
    }

    [Fact]
    public void Send_button_disabled_when_empty()
    {
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([SampleChannel("General")]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();

        cut.Find(".fsh-chat-channel-item").Click();
        cut.WaitForState(() => cut.FindAll(".fsh-chat-messages").Count > 0, timeout: TimeSpan.FromSeconds(5));

        var sendBtn = cut.Find(".mud-icon-button[disabled]");
        sendBtn.ShouldNotBeNull();
    }

    [Fact]
    public void Send_button_enabled_when_message_entered()
    {
        var channel = SampleChannel("General");
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([channel]);
        _chat.ListChannelMessagesAsync(channel.Id, Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();

        cut.Find(".fsh-chat-channel-item").Click();
        cut.WaitForState(() => cut.FindAll(".fsh-chat-messages").Count > 0, timeout: TimeSpan.FromSeconds(5));

        var input = cut.Find("input[placeholder='Type a message...']");
        input.Input("Hello!");

        cut.WaitForState(() =>
        {
            var disabledButtons = cut.FindAll(".mud-icon-button[disabled]");
            return disabledButtons.Count == 0;
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Consecutive_messages_from_same_author_merge_into_one_block()
    {
        var channel = SampleChannel("General");
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([channel]);
        _chat.ListChannelMessagesAsync(channel.Id, Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([SampleMessage("Hello"), SampleMessage("World")]);
        _chat.MarkChannelReadAsync(Arg.Any<Guid>(), Arg.Any<MarkChannelReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();
        cut.Find(".fsh-chat-channel-item").Click();

        cut.WaitForState(() => cut.FindAll(".fsh-chat-message-row").Count == 2, timeout: TimeSpan.FromSeconds(5));
        cut.FindAll(".fsh-chat-message").Count.ShouldBe(1);
        cut.FindAll(".fsh-chat-message-row").Count.ShouldBe(2);
        cut.Markup.ShouldContain("user-1");
    }

    [Fact]
    public void Messages_from_different_authors_render_separate_blocks()
    {
        var channel = SampleChannel("General");
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([channel]);
        _chat.ListChannelMessagesAsync(channel.Id, Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([SampleMessage("Hello", "user-1"), SampleMessage("World", "user-2")]);
        _chat.MarkChannelReadAsync(Arg.Any<Guid>(), Arg.Any<MarkChannelReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();
        cut.Find(".fsh-chat-channel-item").Click();

        cut.WaitForState(() => cut.FindAll(".fsh-chat-message-row").Count == 2, timeout: TimeSpan.FromSeconds(5));
        cut.FindAll(".fsh-chat-message").Count.ShouldBe(2);
    }

    [Fact]
    public void Messages_on_different_days_show_day_separators()
    {
        var channel = SampleChannel("General");
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([channel]);
        _chat.ListChannelMessagesAsync(channel.Id, Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([
                SampleMessage("Old", createdAt: DateTime.UtcNow.AddDays(-3)),
                SampleMessage("New")
            ]);
        _chat.MarkChannelReadAsync(Arg.Any<Guid>(), Arg.Any<MarkChannelReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();
        cut.Find(".fsh-chat-channel-item").Click();

        cut.WaitForState(() => cut.FindAll(".fsh-chat-day-separator").Count == 2, timeout: TimeSpan.FromSeconds(5));
        cut.FindAll(".fsh-chat-day-separator").Count.ShouldBe(2);
        cut.FindAll(".fsh-chat-message").Count.ShouldBe(2);
    }

    [Fact]
    public void Create_channel_button_exists()
    {
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();

        cut.Find("button[aria-label='Create channel']").ShouldNotBeNull();
    }

    [Fact]
    public void Renders_dm_section_with_new_dm_button()
    {
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();

        cut.FindAll(".fsh-chat-section").Count.ShouldBe(2);
        cut.Markup.ShouldContain("Direct Messages");
        cut.Find("button[aria-label='New direct message']").ShouldNotBeNull();
    }

    [Fact]
    public void Splits_channels_and_dms_into_separate_sections()
    {
        var channel = SampleChannel("General");
        var dm = SampleChannel("dm", ChannelType.DirectMessage, unread: 2);
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([channel, dm]);
        _chat.ListChannelMessagesAsync(dm.Id, Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();

        cut.FindAll(".fsh-chat-channel-item").Count.ShouldBe(2);
        cut.FindAll(".fsh-chat-section").Count.ShouldBe(2);
        cut.Markup.ShouldContain("General");
        cut.Markup.ShouldContain("Direct Messages");
    }

    [Fact]
    public void Dm_channel_shows_partner_name_once_resolved()
    {
        var dm = SampleChannel("dm", ChannelType.DirectMessage);
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([dm]);
        _chat.ListChannelMessagesAsync(dm.Id, Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _users.GetAsync("user-1").Returns(Task.FromResult(new UserDto(
            "user-1", "alice", "Alice", "Smith", "alice@example.com", true, true, null, null, false)));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();

        cut.WaitForState(() => cut.Markup.Contains("Alice Smith"), timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void New_dm_button_opens_dialog_with_suggested_users()
    {
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _users.SearchAsync(Arg.Any<SearchRequest>()).Returns(new PagedResult<UserDto>(
            Items: [new UserDto("u-1", "alice", "Alice", "Smith", "alice@example.com", true, true, null, null, false)],
            PageNumber: 1, PageSize: 12, TotalCount: 1, TotalPages: 1, HasNext: false, HasPrevious: false));

        var provider = Render<MudDialogProvider>();
        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();

        cut.Find("button[aria-label='New direct message']").Click();

        provider.WaitForState(() => provider.Markup.Contains("Alice Smith"), timeout: TimeSpan.FromSeconds(5));
        provider.Markup.ShouldContain("New Direct Message");
    }

    [Fact]
    public async Task Clicking_suggested_user_starts_dm_and_selects_channel()
    {
        var dm = SampleChannel("dm", ChannelType.DirectMessage);
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([dm]);
        _chat.FindOrCreateDmAsync(Arg.Any<FindOrCreateDmRequest>())
            .Returns(Task.FromResult(dm.Id));
        _chat.ListChannelMessagesAsync(dm.Id, Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _users.SearchAsync(Arg.Any<SearchRequest>()).Returns(new PagedResult<UserDto>(
            Items: [new UserDto("u-1", "alice", "Alice", "Smith", "alice@example.com", true, true, null, null, false)],
            PageNumber: 1, PageSize: 12, TotalCount: 1, TotalPages: 1, HasNext: false, HasPrevious: false));

        var provider = Render<MudDialogProvider>();
        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();

        cut.Find("button[aria-label='New direct message']").Click();
        provider.WaitForState(() => provider.Markup.Contains("Alice Smith"), timeout: TimeSpan.FromSeconds(5));

        provider.Find(".fsh-chat-dm-user").Click();

        await _chat.Received(1).FindOrCreateDmAsync(Arg.Is<FindOrCreateDmRequest>(r => r.UserIds.Count == 1 && r.UserIds[0] == "u-1"));
        cut.WaitForState(() =>
            Services.GetRequiredService<BunitNavigationManager>().Uri.EndsWith($"/chat/{dm.Id}"),
            timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Scroll_top_loads_older_messages_and_prepends_them()
    {
        var channel = SampleChannel("General");
        var initialPage = BuildMessages(100, "Initial");
        var firstMessage = initialPage.First();
        var history = BuildMessages(2, "Ancient", offsetMinutes: 600);
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([channel]);
        _chat.ListChannelMessagesAsync(channel.Id, Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(initialPage, history);
        _chat.MarkChannelReadAsync(Arg.Any<Guid>(), Arg.Any<MarkChannelReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();
        cut.WaitForState(() => cut.FindAll(".fsh-chat-message-row").Count == 100, timeout: TimeSpan.FromSeconds(5));

        await cut.InvokeAsync(() => cut.Instance.OnScrollTopReached());

        cut.WaitForState(() => cut.FindAll(".fsh-chat-message-row").Count == 102, timeout: TimeSpan.FromSeconds(5));
        await _chat.Received(1).ListChannelMessagesAsync(channel.Id, firstMessage.Id, 50, Arg.Any<CancellationToken>());
        cut.FindAll(".fsh-chat-message-row").First().TextContent.ShouldContain("Ancient 0");
    }

    [Fact]
    public async Task Scroll_top_stops_loading_when_history_exhausted()
    {
        var channel = SampleChannel("General");
        var initialPage = BuildMessages(100, "Initial");
        var firstMessage = initialPage.First();
        var partialPage = BuildMessages(1, "Ancient", offsetMinutes: 600);
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([channel]);
        _chat.ListChannelMessagesAsync(channel.Id, Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(initialPage, partialPage);
        _chat.MarkChannelReadAsync(Arg.Any<Guid>(), Arg.Any<MarkChannelReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();
        cut.WaitForState(() => cut.FindAll(".fsh-chat-message-row").Count == 100, timeout: TimeSpan.FromSeconds(5));

        await cut.InvokeAsync(() => cut.Instance.OnScrollTopReached());
        cut.WaitForState(() => cut.FindAll(".fsh-chat-message-row").Count == 101, timeout: TimeSpan.FromSeconds(5));

        // Second call: partial page → _hasOlder=false → no further fetch.
        await cut.InvokeAsync(() => cut.Instance.OnScrollTopReached());

        await _chat.Received(1).ListChannelMessagesAsync(channel.Id, firstMessage.Id, 50, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Scroll_top_is_ignored_while_already_loading_older()
    {
        var channel = SampleChannel("General");
        var initialPage = BuildMessages(100, "Initial");
        var firstMessage = initialPage.First();
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([channel]);
        _chat.MarkChannelReadAsync(Arg.Any<Guid>(), Arg.Any<MarkChannelReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var gate = new TaskCompletionSource<IReadOnlyList<MessageDto>>();
        _chat.ListChannelMessagesAsync(channel.Id, Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(initialPage), _ => gate.Task);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();
        cut.WaitForState(() => cut.FindAll(".fsh-chat-message-row").Count == 100, timeout: TimeSpan.FromSeconds(5));

        // First scroll-top call starts the gated older-load; second must be ignored while it's in flight.
        _ = cut.Instance.OnScrollTopReached();
        await Task.Delay(50);
        await cut.InvokeAsync(() => cut.Instance.OnScrollTopReached());

        await _chat.Received(1).ListChannelMessagesAsync(channel.Id, firstMessage.Id, 50, Arg.Any<CancellationToken>());

        gate.SetResult([]);
    }

    [Fact]
    public async Task Scroll_top_does_nothing_without_an_active_channel()
    {
        _chat.ListMyChannelsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Chat.ChatPage>();

        await cut.InvokeAsync(() => cut.Instance.OnScrollTopReached());

        await _chat.DidNotReceive().ListChannelMessagesAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    private static IReadOnlyList<MessageDto> BuildMessages(int count, string prefix, int offsetMinutes = 0)
    {
        var list = new List<MessageDto>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(new MessageDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "user-1",
                $"{prefix} {i}",
                null,
                0,
                null,
                null,
                DateTime.UtcNow.AddMinutes(-offsetMinutes - count + i),
                [],
                []));
        }

        return list;
    }

    private sealed class DisposableStub : IDisposable
    {
        public void Dispose() { }
    }
}
