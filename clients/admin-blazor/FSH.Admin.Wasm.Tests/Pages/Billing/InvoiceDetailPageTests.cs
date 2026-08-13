using Bunit;
using FSH.Admin.Wasm.Pages.Billing;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Permissions;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Billing;

public class InvoiceDetailPageTests : TestSetup
{
    private readonly IBillingService _billingService = Substitute.For<IBillingService>();

    public InvoiceDetailPageTests()
    {
        Services.AddSingleton(_billingService);
    }

    private const string InvoiceId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

    private static InvoiceDto SampleInvoice(string status, List<InvoiceLineItemDto>? lineItems = null, DateTime? paidAtUtc = null, DateTime? dueAtUtc = null, string? notes = null) =>
        new(
            Guid.Parse(InvoiceId),
            "acme-corp",
            "INV-2026-07-001",
            2026,
            7,
            "USD",
            315m,
            status,
            new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc),
            null,
            dueAtUtc,
            paidAtUtc,
            null,
            notes,
            lineItems ?? [],
            "Usage",
            null,
            null);

    private static List<InvoiceLineItemDto> SampleLineItems() =>
    [
        new(Guid.NewGuid(), "BaseFee", null, "Pro plan — July 2026", 1m, 290m, 290m),
        new(Guid.NewGuid(), "Overage", "Users", "5 extra users", 5m, 5m, 25m),
    ];

    private void Stub(Guid id, string status, List<InvoiceLineItemDto>? lineItems = null, DateTime? paidAtUtc = null, DateTime? dueAtUtc = null, string? notes = null)
    {
        _billingService.GetInvoiceByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(SampleInvoice(status, lineItems, paidAtUtc, dueAtUtc, notes));
    }

    [Fact]
    public void Renders_invoice_with_line_items_and_subtotal()
    {
        Stub(Guid.Parse(InvoiceId), "Draft", SampleLineItems());
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(BillingPermissions.Manage);

        var cut = Render<InvoiceDetailPage>(p => p.Add(x => x.Id, InvoiceId));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("USD 315.00"));
        cut.Markup.ShouldContain("INV-2026-07-001");
        cut.Markup.ShouldContain("Draft");
        cut.Markup.ShouldContain("Usage");
        cut.Markup.ShouldContain("Pro plan — July 2026");
        cut.Markup.ShouldContain("BaseFee");
        cut.Markup.ShouldContain("Overage");
        cut.Markup.ShouldContain("Users");
        cut.Markup.ShouldContain("5 × USD 5.00");
        cut.Markup.ShouldContain("subtotal");
        cut.Markup.ShouldContain("2 lines");
        cut.Markup.ShouldContain("acme-corp");
        cut.Markup.ShouldContain("period 2026-07");
    }

    [Fact]
    public void Issue_is_enabled_for_draft_but_mark_paid_and_void_are_disabled()
    {
        Stub(Guid.Parse(InvoiceId), "Draft", SampleLineItems());
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(BillingPermissions.Manage);

        var cut = Render<InvoiceDetailPage>(p => p.Add(x => x.Id, InvoiceId));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Issue invoice"));

        ButtonIsDisabled(cut, "Issue invoice").ShouldBeFalse();
        ButtonIsDisabled(cut, "Mark as paid").ShouldBeTrue();
        ButtonIsDisabled(cut, "Void invoice").ShouldBeFalse();
    }

    [Fact]
    public async Task Issue_button_calls_service_and_reloads()
    {
        Stub(Guid.Parse(InvoiceId), "Draft", SampleLineItems());
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(BillingPermissions.Manage);

        var cut = Render<InvoiceDetailPage>(p => p.Add(x => x.Id, InvoiceId));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Issue invoice"));

        cut.FindAll("button").First(b => b.TextContent.Contains("Issue invoice")).Click();

        await cut.WaitForAssertionAsync(() =>
            _billingService.Received(1).IssueInvoiceAsync(Guid.Parse(InvoiceId), null, Arg.Any<CancellationToken>()));
        _ = _billingService.Received(2).GetInvoiceByIdAsync(Guid.Parse(InvoiceId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Void_calls_service_with_reason()
    {
        Stub(Guid.Parse(InvoiceId), "Issued", SampleLineItems());
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(BillingPermissions.Manage);

        var cut = Render<InvoiceDetailPage>(p => p.Add(x => x.Id, InvoiceId));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Void invoice"));

        // Find the void reason input by placeholder (the date picker input is first)
        var reasonInput = cut.FindAll("input").First(i => i.GetAttribute("placeholder")?.Contains("duplicate") == true);
        reasonInput.Change("duplicate");
        cut.FindAll("button").First(b => b.TextContent.Contains("Void invoice")).Click();

        await cut.WaitForAssertionAsync(() =>
            _billingService.Received(1).VoidInvoiceAsync(Guid.Parse(InvoiceId), "duplicate", Arg.Any<CancellationToken>()));
    }

    [Fact]
    public void Paid_invoice_disables_issue_and_void()
    {
        Stub(Guid.Parse(InvoiceId), "Paid", SampleLineItems(), paidAtUtc: new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc));
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(BillingPermissions.Manage);

        var cut = Render<InvoiceDetailPage>(p => p.Add(x => x.Id, InvoiceId));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("paid Jul 10, 2026"));

        ButtonIsDisabled(cut, "Issue invoice").ShouldBeTrue();
        ButtonIsDisabled(cut, "Mark as paid").ShouldBeTrue();
        ButtonIsDisabled(cut, "Void invoice").ShouldBeTrue();
    }

    [Fact]
    public void Actions_hidden_without_manage_permission()
    {
        Stub(Guid.Parse(InvoiceId), "Draft", SampleLineItems());

        var cut = Render<InvoiceDetailPage>(p => p.Add(x => x.Id, InvoiceId));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("INV-2026-07-001"));

        cut.Markup.ShouldNotContain("Issue invoice");
        cut.Markup.ShouldNotContain("Mark as paid");
        cut.Markup.ShouldNotContain("Void invoice");
        cut.Markup.ShouldNotContain("Download PDF");
    }

    [Fact]
    public void Load_failure_shows_error_band()
    {
        _billingService.GetInvoiceByIdAsync(Guid.Parse(InvoiceId), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<InvoiceDto>(new InvalidOperationException("boom")));

        var cut = Render<InvoiceDetailPage>(p => p.Add(x => x.Id, InvoiceId));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to load invoice: boom"));
    }

    [Fact]
    public void Invalid_id_shows_error()
    {
        var cut = Render<InvoiceDetailPage>(p => p.Add(x => x.Id, "not-a-guid"));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Invalid invoice identifier."));
    }

    private static bool ButtonIsDisabled(IRenderedComponent<InvoiceDetailPage> cut, string text)
    {
        var button = cut.FindAll("button").First(b => b.TextContent.Contains(text));
        return button.HasAttribute("disabled");
    }
}
