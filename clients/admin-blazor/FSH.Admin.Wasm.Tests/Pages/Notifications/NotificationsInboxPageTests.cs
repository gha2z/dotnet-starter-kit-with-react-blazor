using Bunit;
using FSH.Admin.Wasm.Pages.Notifications;
using FSH.BlazorShared.Models.Notifications;
using FSH.BlazorShared.Realtime;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Notifications;

public class NotificationsInboxPageTests : TestSetup
{
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly IHubConnectionService _hub = Substitute.For<IHubConnectionService>();

    public NotificationsInboxPageTests()
    {
        Services.AddSingleton(_notificationService);
        Services.AddSingleton(_hub);
    }

    private static NotificationDto Sample(
        Guid id,
        string source = "Billing",
        string type = "InvoiceIssued",
        bool read = false) =>
        new(id, type, "Invoice INV-2026-001 issued", "A new invoice is ready", null, source, "{}",
            read ? DateTime.UtcNow : null, new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc));

    private void StubList(params NotificationDto[] notifications) =>
        _notificationService.ListAsync(Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(notifications.ToList());

    [Fact]
    public void Renders_notifications_with_source_title_and_type()
    {
        StubList(Sample(Guid.NewGuid(), "Billing", "InvoiceIssued"));
        _notificationService.MarkAllReadAsync(Arg.Any<CancellationToken>()).Returns(0);

        var cut = Render<NotificationsInboxPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Invoice INV-2026-001 issued"));
        cut.Markup.ShouldContain("Billing");
        cut.Markup.ShouldContain("InvoiceIssued");
    }

    [Fact]
    public void Shows_inbox_zero_empty_state_when_no_unread()
    {
        StubList();
        _notificationService.MarkAllReadAsync(Arg.Any<CancellationToken>()).Returns(0);

        var cut = Render<NotificationsInboxPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Nothing unread."));
        cut.Markup.ShouldContain("inbox zero");
    }

    [Fact]
    public void Shows_error_band_when_load_fails()
    {
        _notificationService.ListAsync(Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<NotificationDto>>(new Exception("boom")));

        var cut = Render<NotificationsInboxPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("boom"));
    }

    [Fact]
    public void Marking_all_read_updates_rows()
    {
        var id = Guid.NewGuid();
        StubList(Sample(id, "Billing", "InvoiceIssued", read: false));
        _notificationService.MarkAllReadAsync(Arg.Any<CancellationToken>()).Returns(1);

        var cut = Render<NotificationsInboxPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Mark all read"));

        cut.FindAll("button").First(b => b.TextContent.Contains("Mark all read")).Click();

        cut.WaitForAssertion(() =>
            _notificationService.Received(1).MarkAllReadAsync(Arg.Any<CancellationToken>()));
    }
}
