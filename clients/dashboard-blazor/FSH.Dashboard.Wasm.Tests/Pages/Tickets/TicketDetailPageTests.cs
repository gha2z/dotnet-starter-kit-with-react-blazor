using Bunit;
using FSH.BlazorShared.Models.Tickets;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Tickets;

public sealed class TicketDetailPageTests : TestSetup
{
    private readonly ITicketService _tickets = Substitute.For<ITicketService>();

    public TicketDetailPageTests()
    {
        Services.AddSingleton(_tickets);
    }

    private static TicketDto SampleTicket(
        TicketStatus status = TicketStatus.Open,
        TicketPriority priority = TicketPriority.Medium,
        string? resolutionNote = null) =>
        new(Guid.NewGuid(), "TK-001", "Login fails", "Description here",
            status, priority, Guid.NewGuid(), null, resolutionNote,
            DateTime.UtcNow.AddDays(-5), DateTime.UtcNow, null, null, 2);

    [Fact]
    public void Renders_ticket_detail_with_data()
    {
        var ticket = SampleTicket();
        _tickets.GetTicketByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ticket);
        _tickets.ListTicketCommentsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Tickets.TicketDetailPage>(parameters =>
            parameters.Add(p => p.Id, ticket.Id.ToString()));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Login fails");
            cut.Markup.ShouldContain("TK-001");
            cut.Markup.ShouldContain("Description here");
            cut.Markup.ShouldContain("Open");
            cut.Markup.ShouldContain("Medium");
        });
    }

    [Fact]
    public void Shows_error_band_when_service_fails()
    {
        _tickets.GetTicketByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TicketDto>(new InvalidOperationException("boom")));
        _tickets.ListTicketCommentsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Tickets.TicketDetailPage>(parameters =>
            parameters.Add(p => p.Id, Guid.NewGuid().ToString()));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("boom"));
    }

    [Fact]
    public void Shows_not_found_when_ticket_is_null()
    {
        _tickets.GetTicketByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TicketDto?)null!);
        _tickets.ListTicketCommentsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Tickets.TicketDetailPage>(parameters =>
            parameters.Add(p => p.Id, Guid.NewGuid().ToString()));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("no longer exists"));
    }

    [Fact]
    public void Shows_resolve_button_for_open_ticket()
    {
        var ticket = SampleTicket(status: TicketStatus.Open);
        _tickets.GetTicketByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ticket);
        _tickets.ListTicketCommentsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Tickets.TicketDetailPage>(parameters =>
            parameters.Add(p => p.Id, ticket.Id.ToString()));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Resolve"));
    }

    [Fact]
    public void Shows_reopen_button_for_resolved_ticket()
    {
        var ticket = SampleTicket(status: TicketStatus.Resolved);
        _tickets.GetTicketByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ticket);
        _tickets.ListTicketCommentsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Tickets.TicketDetailPage>(parameters =>
            parameters.Add(p => p.Id, ticket.Id.ToString()));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Reopen"));
    }

    [Fact]
    public void Hides_resolve_button_for_closed_ticket()
    {
        var ticket = SampleTicket(status: TicketStatus.Closed);
        _tickets.GetTicketByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ticket);
        _tickets.ListTicketCommentsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Tickets.TicketDetailPage>(parameters =>
            parameters.Add(p => p.Id, ticket.Id.ToString()));

        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("Resolve"));
    }

    [Fact]
    public void Shows_resolution_note_when_present()
    {
        var ticket = SampleTicket(status: TicketStatus.Resolved, resolutionNote: "Fixed in v2");
        _tickets.GetTicketByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ticket);
        _tickets.ListTicketCommentsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Tickets.TicketDetailPage>(parameters =>
            parameters.Add(p => p.Id, ticket.Id.ToString()));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Resolution");
            cut.Markup.ShouldContain("Fixed in v2");
        });
    }

    [Fact]
    public void Shows_comments_when_loaded()
    {
        var ticket = SampleTicket();
        var commentId = Guid.NewGuid();
        _tickets.GetTicketByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ticket);
        _tickets.ListTicketCommentsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([new TicketCommentDto(commentId, ticket.Id, Guid.NewGuid(), "Thanks for reporting", DateTime.UtcNow)]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Tickets.TicketDetailPage>(parameters =>
            parameters.Add(p => p.Id, ticket.Id.ToString()));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Thanks for reporting"));
    }
}
