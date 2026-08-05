using FSH.Admin.Wasm.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;

namespace FSH.Admin.Wasm.E2E.Tests;

/// <summary>
/// Inactivity timeout: warning dialog → dismiss → stays logged in.
///
/// SKIPPED — the feature does not exist in the admin-blazor app yet. The React
/// apps ship `use-inactivity-timeout.ts`, and BlazorShared ships a dormant
/// `InactivityTimerService` (10-minute countdown, warning at 60s, OnWarning /
/// OnTimeout events), but nothing wires it up: it is not registered in
/// Program.cs, no layout/component consumes OnWarning, and there is no warning
/// dialog UI. There is therefore nothing to drive from Playwright — the
/// 9-minute wait alone would be impractical headless, and asserting on a
/// nonexistent dialog would be testing nothing.
///
/// Blocked on the app-side feature (inactivity wiring + warning dialog in
/// MainLayout). Once landed, this test should seed a session, idle past the
/// warning threshold (or use Playwright clock if the timer is JS-backed),
/// assert the warning dialog, dismiss it, and assert the session survives.
/// </summary>
[Collection("e2e")]
public sealed class InactivityTimeoutTests(BlazorAppServerFixture fixture) : IAsyncDisposable
{
    private readonly IBrowser _browser = fixture.Browser;
    private IBrowserContext? _context;
    private IPage? _page;

    private const string SkipReason =
        "InactivityTimerService is not wired into the admin-blazor app (not registered in " +
        "Program.cs, no OnWarning consumer, no warning dialog UI) — nothing to exercise. " +
        "See the class-level comment for what the test must assert once the feature lands.";

    [Fact(Skip = SkipReason)]
    public async Task Inactivity_WarningDialog_Dismiss_StaysLoggedIn()
    {
        _context = await _browser.NewContextAsync();
        await E2EHelpers.SeedAuthedSessionAsync(_context);
        _page = await _context.NewPageAsync();
        await E2EHelpers.InstallShellMocksAsync(_page);
        await E2EHelpers.MockOverviewAsync(_page);

        await _page.GotoAsync(BlazorAppServerFixture.ServerUrl + "/");
        await E2EHelpers.WaitForBlazorReadyAsync(_page);
        await Expect(_page.Locator(".fsh-sidebar")).ToBeVisibleAsync();

        // Warning dialog appears near the timeout threshold…
        var warning = _page.Locator(".mud-dialog", new PageLocatorOptions { HasText = "inactive" });
        await Expect(warning).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });

        // …dismissing it resets the timer and keeps the session alive.
        await warning.GetByRole(AriaRole.Button, new() { Name = "Stay signed in" }).ClickAsync();
        await Expect(warning).ToBeHiddenAsync();
        await Expect(_page.Locator(".fsh-sidebar")).ToBeVisibleAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_page is not null)
        {
            await _page.CloseAsync();
        }

        if (_context is not null)
        {
            await _context.DisposeAsync();
        }
    }
}
