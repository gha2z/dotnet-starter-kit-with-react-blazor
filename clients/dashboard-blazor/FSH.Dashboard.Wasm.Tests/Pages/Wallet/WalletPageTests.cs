using Bunit;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Wallet;

public sealed class WalletPageTests : TestSetup
{
    private readonly IBillingService _billing = Substitute.For<IBillingService>();

    public WalletPageTests()
    {
        Services.AddSingleton(_billing);
    }

    private static WalletDto SampleWallet(decimal balance = 25m, string status = "Active") =>
        new(Guid.NewGuid(), "acme", "USD", balance, status, DateTime.UtcNow, []);

    private static TopupRequestDto SampleTopupRequest(string status = "Completed", decimal amount = 50m) =>
        new(Guid.NewGuid(), "acme", amount, "USD", "Monthly top-up", status, null, "admin@acme.test", null, DateTime.UtcNow.AddDays(-3), null, null);

    [Fact]
    public void Renders_balance_and_topup_requests()
    {
        _billing.GetMyWalletAsync(Arg.Any<CancellationToken>()).Returns(SampleWallet(250m));
        _billing.GetTopupRequestsAsync(1, 20, null, null, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TopupRequestDto>(
                [SampleTopupRequest("Completed", 100m), SampleTopupRequest("Pending", 50m)],
                1, 20, 2, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Wallet.WalletPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("WhatsApp wallet");
            cut.Markup.ShouldContain("USD 250.00");
            cut.Markup.ShouldContain("Completed");
            cut.Markup.ShouldContain("Pending");
            cut.Markup.ShouldContain("Monthly top-up");
            cut.Markup.ShouldContain("My top-up requests");
        });
    }

    [Fact]
    public void Shows_low_balance_hint_when_balance_below_threshold()
    {
        _billing.GetMyWalletAsync(Arg.Any<CancellationToken>()).Returns(SampleWallet(5m));
        _billing.GetTopupRequestsAsync(1, 20, null, null, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TopupRequestDto>([], 1, 20, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Wallet.WalletPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("running low"));
    }

    [Fact]
    public void Shows_empty_wallet_hint_when_balance_zero()
    {
        _billing.GetMyWalletAsync(Arg.Any<CancellationToken>()).Returns(SampleWallet(0m));
        _billing.GetTopupRequestsAsync(1, 20, null, null, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TopupRequestDto>([], 1, 20, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Wallet.WalletPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("wallet is empty"));
    }

    [Fact]
    public void Renders_empty_state_when_no_topup_requests()
    {
        _billing.GetMyWalletAsync(Arg.Any<CancellationToken>()).Returns(SampleWallet());
        _billing.GetTopupRequestsAsync(1, 20, null, null, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TopupRequestDto>([], 1, 20, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Wallet.WalletPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No top-up requests yet"));
    }

    [Fact]
    public void Renders_error_band_when_wallet_fails()
    {
        _billing.GetMyWalletAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<WalletDto>(new InvalidOperationException("boom")));
        _billing.GetTopupRequestsAsync(1, 20, null, null, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TopupRequestDto>([], 1, 20, 0, 1, false, false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Wallet.WalletPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("boom"));
    }
}
