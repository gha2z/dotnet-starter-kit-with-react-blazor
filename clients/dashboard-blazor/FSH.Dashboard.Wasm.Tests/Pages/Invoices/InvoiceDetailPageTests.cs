using Bunit;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Invoices;

public sealed class InvoiceDetailPageTests : TestSetup
{
    private readonly IBillingService _billing = Substitute.For<IBillingService>();

    public InvoiceDetailPageTests()
    {
        Services.AddSingleton(_billing);
    }

    private static InvoiceDto SampleInvoice() =>
        new(Guid.NewGuid(), "acme", "INV-2026-001", 2026, 7, "USD", 150m, "Paid",
            new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc),
            null, "Settled", [new InvoiceLineItemDto(Guid.NewGuid(), "Subscription", null, "Pro plan", 1m, 150m, 150m)],
            "Subscription", new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Renders_invoice_header_line_items_and_details()
    {
        _billing.GetInvoiceByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(SampleInvoice());

        var cut = Render<FSH.Dashboard.Wasm.Pages.Invoices.InvoiceDetailPage>(parameters => parameters
            .Add(p => p.Id, Guid.NewGuid().ToString()));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Back to invoices");
            cut.Markup.ShouldContain("INV-2026-001");
            cut.Markup.ShouldContain("Paid");
            cut.Markup.ShouldContain("Pro plan");
            cut.Markup.ShouldContain("USD 150.00");
            cut.Markup.ShouldContain("Subscription");
        });
    }

    [Fact]
    public void Renders_error_band_on_failure()
    {
        _billing.GetInvoiceByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<InvoiceDto>(new InvalidOperationException("boom")));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Invoices.InvoiceDetailPage>(parameters => parameters
            .Add(p => p.Id, Guid.NewGuid().ToString()));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("boom"));
    }

    [Fact]
    public void Renders_not_found_when_null()
    {
        _billing.GetInvoiceByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InvoiceDto>(null!));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Invoices.InvoiceDetailPage>(parameters => parameters
            .Add(p => p.Id, Guid.NewGuid().ToString()));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Invoice not found"));
    }
}
