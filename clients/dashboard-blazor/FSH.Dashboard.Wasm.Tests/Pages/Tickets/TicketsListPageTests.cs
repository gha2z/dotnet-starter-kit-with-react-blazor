using Bunit;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Tickets;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Tickets;

public sealed class TicketsListPageTests : TestSetup
{
    private readonly ITicketService _tickets = Substitute.For<ITicketService>();

    public TicketsListPageTests()
    {
        Services.AddSingleton(_tickets);
    }

    private static TicketDto SampleTicket(
        string title = "Login fails",
        TicketStatus status = TicketStatus.Open,
        TicketPriority priority = TicketPriority.Medium) =>
        new(Guid.NewGuid(), "TK-001", title, "Description here", status, priority,
            Guid.NewGuid(), null, null, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, null, null, 3);

    [Fact]
    public void Renders_ticket_list_with_data()
    {
        _tickets.SearchTicketsAsync(
            Arg.Any<string?>(), Arg.Any<TicketStatus?>(), Arg.Any<TicketPriority?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TicketDto>(
                [SampleTicket("Login fails"), SampleTicket("Slow dashboard", TicketStatus.InProgress, TicketPriority.High)],
                1, 20, 2, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Tickets.TicketsListPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Login fails");
            cut.Markup.ShouldContain("TK-001");
            cut.Markup.ShouldContain("Slow dashboard");
            cut.Markup.ShouldContain("Medium");
            cut.Markup.ShouldContain("High");
        });
    }

    [Fact]
    public void Shows_empty_state_when_no_tickets()
    {
        _tickets.SearchTicketsAsync(
            Arg.Any<string?>(), Arg.Any<TicketStatus?>(), Arg.Any<TicketPriority?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TicketDto>([], 1, 20, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Tickets.TicketsListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No tickets yet"));
    }

    [Fact]
    public void Shows_error_band_when_service_fails()
    {
        _tickets.SearchTicketsAsync(
            Arg.Any<string?>(), Arg.Any<TicketStatus?>(), Arg.Any<TicketPriority?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PagedResult<TicketDto>>(new InvalidOperationException("boom")));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Tickets.TicketsListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("boom"));
    }

    [Fact]
    public void Shows_unassigned_label_when_no_assignee()
    {
        _tickets.SearchTicketsAsync(
            Arg.Any<string?>(), Arg.Any<TicketStatus?>(), Arg.Any<TicketPriority?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TicketDto>(
                [SampleTicket()],
                1, 20, 1, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Tickets.TicketsListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Unassigned"));
    }

    [Fact]
    public void Shows_assignee_when_ticket_has_one()
    {
        var assigneeId = Guid.NewGuid();
        var ticket = SampleTicket();
        var assigned = ticket with { AssignedToUserId = assigneeId };
        _tickets.SearchTicketsAsync(
            Arg.Any<string?>(), Arg.Any<TicketStatus?>(), Arg.Any<TicketPriority?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TicketDto>([assigned], 1, 20, 1, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Tickets.TicketsListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain(assigneeId.ToString()[..8]));
    }

    [Fact]
    public void Reloads_with_search_term_after_debounce()
    {
        var calls = 0;
        _tickets.SearchTicketsAsync(
            Arg.Any<string?>(), Arg.Any<TicketStatus?>(), Arg.Any<TicketPriority?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new PagedResult<TicketDto>(
                calls++ == 0 ? [SampleTicket("Login fails")] : [SampleTicket("Payments crash")],
                1, 20, 1, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Tickets.TicketsListPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Login fails"));

        cut.Find("input[placeholder='Find by number, title, or description.']").Input("payments");

        cut.WaitForAssertion(
            () => _tickets.Received(2)
                .SearchTicketsAsync(Arg.Any<string?>(), Arg.Any<TicketStatus?>(), Arg.Any<TicketPriority?>(),
                    Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<int>(),
                    Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()),
            TimeSpan.FromSeconds(2));

        _tickets.Received().SearchTicketsAsync(
            "payments", Arg.Any<TicketStatus?>(), Arg.Any<TicketPriority?>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Payments crash"), TimeSpan.FromSeconds(2));
    }
}
