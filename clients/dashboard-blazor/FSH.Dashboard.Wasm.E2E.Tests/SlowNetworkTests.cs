using FSH.Dashboard.Wasm.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.E2E.Tests;

/// <summary>
/// Slow-network behavior: with 3G-ish throttling the branded splash must stay
/// visible while the WASM framework downloads, then boot must complete cleanly
/// (no #blazor-error-ui) and the login page renders.
/// </summary>
[Collection("e2e")]
public sealed class SlowNetworkTests(BlazorAppServerFixture fixture) : IAsyncLifetime
{
    private IBrowserContext _context = default!;
    private IPage _page = default!;
    private ICDPSession? _cdp;

    public async Task InitializeAsync()
    {
        _context = await fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = "en-US",
        });
        _page = await _context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_cdp is not null)
        {
            await _cdp.DetachAsync();
        }

        await _context.CloseAsync();
    }

    [Fact]
    public async Task SlowNetwork_SplashShown_AndBootCompletesCleanly()
    {
        // 3G-ish: ~1 MB/s with 250 ms latency (fast enough to boot in-test,
        // slow enough that the splash is deterministically on screen first).
        _cdp = await _context.NewCDPSessionAsync(_page);
        await _cdp.SendAsync("Network.enable");
        await _cdp.SendAsync("Network.emulateNetworkConditions", new Dictionary<string, object>
        {
            ["offline"] = false,
            ["latency"] = 250,
            ["downloadThroughput"] = 1024 * 1024,
            ["uploadThroughput"] = 512 * 1024,
        });

        await _page.GotoAsync(
            $"{BlazorAppServerFixture.ServerUrl}/login",
            new PageGotoOptions { WaitUntil = WaitUntilState.Commit });

        await Expect(_page.Locator(".fsh-loading-brand")).ToBeVisibleAsync();
        await _page.GetByText("Loading your dashboard…").WaitForAsync();

        await _page.WaitForSelectorAsync(".mud-theme-provider", new PageWaitForSelectorOptions
        {
            Timeout = 180_000,
            State = WaitForSelectorState.Attached,
        });

        var errorUi = _page.Locator("#blazor-error-ui");
        (await errorUi.IsVisibleAsync()).ShouldBeFalse("the error UI must not surface on a slow network");
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Welcome back" })).ToBeVisibleAsync();
        await Expect(_page.Locator(".fsh-loading-container")).ToHaveCountAsync(0);
    }
}
