using Bunit;
using FSH.BlazorShared.Sse;
using FSH.Dashboard.Wasm.Pages.Activity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Activity;

public sealed class ActivityPageTests : TestSetup
{
    private readonly FakeSseService _sse = new();

    public ActivityPageTests()
    {
        Services.AddSingleton<ISseService>(_sse);
    }

    [Fact]
    public void Renders_offline_badge_and_empty_state_when_disconnected_and_no_events()
    {
        _sse.IsConnected = false;

        var cut = Render<ActivityPage>();

        cut.Markup.ShouldContain("offline");
        cut.Markup.ShouldContain("No events yet");
        cut.Markup.ShouldContain("The activity stream is not connected");
    }

    [Fact]
    public void Renders_streaming_badge_when_connected_even_with_no_events()
    {
        _sse.IsConnected = true;

        var cut = Render<ActivityPage>();

        cut.Markup.ShouldContain("streaming");
        cut.Markup.ShouldContain("Listening for activity");
        cut.Markup.ShouldContain("The stream is open");
    }

    [Fact]
    public void Prepend_new_events_and_shows_payload_and_entity()
    {
        _sse.IsConnected = true;
        var cut = Render<ActivityPage>();

        _sse.Publish(new SseEvent("UserLogin", """{"entityId":"usr_123","message":"signed in"}"""));
        _sse.Publish(new SseEvent("InvoiceCreated", """{"aggregateId":"inv_456"}"""));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("UserLogin"));
        cut.Markup.ShouldContain("InvoiceCreated");
        // newest first
        cut.Markup.IndexOf("InvoiceCreated", StringComparison.Ordinal)
            .ShouldBeLessThan(cut.Markup.IndexOf("UserLogin", StringComparison.Ordinal));
        cut.Markup.ShouldContain("usr_123");
        cut.Markup.ShouldContain("inv_456");
        cut.Markup.ShouldContain("2 events shown");
        cut.Markup.ShouldContain("· 2 total");
    }

    [Fact]
    public void Caps_event_list_at_200_and_keeps_total_count()
    {
        _sse.IsConnected = true;
        var cut = Render<ActivityPage>();

        for (var i = 0; i < 205; i++)
        {
            _sse.Publish(new SseEvent($"Ev{i}", $"{{\"id\":\"id_{i}\"}}"));
        }

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("205 total"));
        cut.Markup.ShouldContain("200 events shown");
        // oldest event was trimmed
        cut.Markup.ShouldNotContain("Ev0");
        cut.Markup.ShouldContain("Ev204");
    }

    [Fact]
    public void Payload_summary_shows_raw_for_plain_string_payload()
    {
        _sse.IsConnected = true;
        var cut = Render<ActivityPage>();

        _sse.Publish(new SseEvent("TokenRefreshed", "plain text payload"));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("plain text payload"));
    }

    [Fact]
    public void Entity_label_falls_back_to_em_dash_when_no_id_field()
    {
        _sse.IsConnected = true;
        var cut = Render<ActivityPage>();

        _sse.Publish(new SseEvent("SecurityAlert", """{"message":"no id here"}"""));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("—"));
    }

    [Fact]
    public void Subscribes_to_sse_on_render()
    {
        var cut = Render<ActivityPage>();

        _sse.SubscriptionCount.ShouldBe(1);
    }

    /// <summary>
    /// Test double that lets tests push SSE events and toggle connection state.
    /// </summary>
    private sealed class FakeSseService : ISseService
    {
        private readonly List<IObserver<SseEvent>> _observers = new();

        public IObservable<SseEvent> Messages => new Observable(this);
        public bool IsConnected { get; set; }
        public int SubscriptionCount => _observers.Count;
        public event Action? ConnectionChanged;

        public Task StartAsync(CancellationToken ct = default)
        {
            ConnectionChanged?.Invoke();
            return Task.CompletedTask;
        }

        public Task StopAsync() => Task.CompletedTask;

        public void Publish(SseEvent ev)
        {
            foreach (var observer in _observers.ToList())
            {
                observer.OnNext(ev);
            }
        }

        private sealed class Observable(FakeSseService parent) : IObservable<SseEvent>
        {
            public IDisposable Subscribe(IObserver<SseEvent> observer)
            {
                parent._observers.Add(observer);
                return new Subscription(parent, observer);
            }
        }

        private sealed class Subscription(FakeSseService parent, IObserver<SseEvent> observer) : IDisposable
        {
            public void Dispose() => parent._observers.Remove(observer);
        }
    }
}
