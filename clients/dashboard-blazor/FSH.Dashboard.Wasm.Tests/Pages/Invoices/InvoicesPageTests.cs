using Bunit;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Invoices;

public sealed class InvoicesPageTests : TestSetup
{
    private readonly IBillingService _billing = Substitute.For<IBillingService>();

    public InvoicesPageTests()
    {
        Services.AddSingleton(_billing);
    }

    private static InvoiceDto SampleInvoice(string invoiceNumber = "INV-2026-001", string status = "Paid") =>
        new(Guid.NewGuid(), "acme", invoiceNumber, 2026, 7, "USD", 120.50m, status, DateTime.UtcNow.AddDays(-2),
            null, DateTime.UtcNow.AddDays(-1), null, null, null, [], "Subscription", null, null);

    [Fact]
    public void Renders_invoices_with_status_pills()
    {
        _billing.GetMyInvoicesAsync(1, 20, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InvoiceDto>(
                [SampleInvoice("INV-2026-001", "Paid"), SampleInvoice("INV-2026-002", "Issued")],
                1, 20, 2, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Invoices.InvoicesPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Invoices");
            cut.Markup.ShouldContain("INV-2026-001");
            cut.Markup.ShouldContain("INV-2026-002");
            cut.Markup.ShouldContain("Paid");
            cut.Markup.ShouldContain("Issued");
            cut.Markup.ShouldContain("USD 120.50");
        });
    }

    [Fact]
    public void Renders_empty_state_when_no_invoices()
    {
        _billing.GetMyInvoicesAsync(1, 20, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InvoiceDto>([], 1, 20, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Invoices.InvoicesPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No invoices yet"));
    }

    [Fact]
    public void Renders_error_band_on_failure()
    {
        _billing.GetMyInvoicesAsync(1, 20, null, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PagedResult<InvoiceDto>>(new InvalidOperationException("boom")));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Invoices.InvoicesPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("boom"));
    }

    [Fact]
    public void Search_filters_invoices_client_side()
    {
        _billing.GetMyInvoicesAsync(1, 20, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InvoiceDto>(
                [SampleInvoice("INV-2026-001", "Paid"), SampleInvoice("INV-2026-002", "Issued")],
                1, 20, 2, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Invoices.InvoicesPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("INV-2026-001"));

        var searchInput = cut.Find("input");
        searchInput.Input("INV-2026-002");

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("INV-2026-002");
            cut.Markup.ShouldNotContain("INV-2026-001");
        });
    }
}
