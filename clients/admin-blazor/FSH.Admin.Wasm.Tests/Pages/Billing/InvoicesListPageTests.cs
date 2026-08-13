using Bunit;
using FSH.Admin.Wasm.Pages.Billing;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Billing;

public class InvoicesListPageTests : TestSetup
{
    private readonly IBillingService _billingService = Substitute.For<IBillingService>();

    public InvoicesListPageTests()
    {
        Services.AddSingleton(_billingService);
    }

    private static InvoiceDto SampleInvoice(
        Guid id,
        string invoiceNumber,
        string status,
        decimal subtotal,
        DateTime? dueAtUtc = null,
        DateTime? paidAtUtc = null,
        string tenantId = "acme-corp") =>
        new(
            id,
            tenantId,
            invoiceNumber,
            2026,
            7,
            "USD",
            subtotal,
            status,
            new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc),
            null,
            dueAtUtc,
            paidAtUtc,
            null,
            null,
            [],
            "Usage",
            null,
            null);

    private static PagedResult<InvoiceDto> Page(params InvoiceDto[] invoices) =>
        new([.. invoices], 1, 20, invoices.Length, invoices.Length == 0 ? 0 : 1, false, false);

    [Fact]
    public void Renders_invoices_with_kpi_totals()
    {
        _billingService.GetInvoicesAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Page(
                SampleInvoice(Guid.NewGuid(), "INV-2026-07-001", "Draft", 100m),
                SampleInvoice(Guid.NewGuid(), "INV-2026-07-002", "Issued", 250m, dueAtUtc: new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc)),
                SampleInvoice(Guid.NewGuid(), "INV-2026-07-003", "Paid", 75m, paidAtUtc: new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc))));

        var cut = Render<InvoicesListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("INV-2026-07-002"));
        cut.Markup.ShouldContain("INV-2026-07-001");
        cut.Markup.ShouldContain("INV-2026-07-003");
        cut.Markup.ShouldContain("Issued");
        cut.Markup.ShouldContain("Paid");
        cut.Markup.ShouldContain("Draft");
        cut.Markup.ShouldContain("Usage");
        cut.Markup.ShouldContain("USD 425.00");
        cut.Markup.ShouldContain("USD 250.00");
        cut.Markup.ShouldContain("USD 75.00");
        cut.Markup.ShouldContain("1 invoice");
        cut.Markup.ShouldContain("3 total");
        cut.Markup.ShouldContain("Page 1 of 1 · 3 total");
        cut.Markup.ShouldContain("period 2026-07");
        cut.Markup.ShouldContain("acme-corp");
        cut.Markup.ShouldContain("due Jul 15, 2026");
        cut.Markup.ShouldContain("paid Jul 10, 2026");
    }

    [Fact]
    public void Row_click_navigates_to_invoice_detail()
    {
        var invoiceId = Guid.NewGuid();
        _billingService.GetInvoicesAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Page(SampleInvoice(invoiceId, "INV-2026-07-001", "Draft", 100m)));

        var cut = Render<InvoicesListPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("INV-2026-07-001"));

        cut.FindAll("div.cursor-pointer").First().Click();

        var nav = Services.GetRequiredService<Bunit.TestDoubles.BunitNavigationManager>();
        nav.History.Last().Uri.ShouldEndWith($"/billing/invoices/{invoiceId}");
    }

    [Fact]
    public void Empty_state_when_no_invoices_match()
    {
        _billingService.GetInvoicesAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Page());

        var cut = Render<InvoicesListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No invoices found."));
        cut.Markup.ShouldContain("No invoices match the current filters.");
    }

    [Fact]
    public void Load_failure_shows_error_band()
    {
        _billingService.GetInvoicesAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PagedResult<InvoiceDto>>(new InvalidOperationException("boom")));

        var cut = Render<InvoicesListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to load invoices: boom"));
    }
}
