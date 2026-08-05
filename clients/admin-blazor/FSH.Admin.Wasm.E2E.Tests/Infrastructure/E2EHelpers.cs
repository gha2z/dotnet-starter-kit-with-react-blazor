using System.Text.Json;
using Microsoft.Playwright;

namespace FSH.Admin.Wasm.E2E.Tests.Infrastructure;

/// <summary>
/// Playwright test helpers mirroring the React apps' route-mocked E2E harness
/// (clients/admin/tests/helpers/*): authed-session seeding via localStorage
/// init scripts, route-level API mocks, and the admin-blazor shell mocks.
///
/// The WASM app talks to the API cross-origin (https://localhost:7030 per
/// wwwroot/config.json), so every fulfilled response carries CORS headers and
/// OPTIONS preflights are answered — without a real backend, unmocked requests
/// would fail at the browser CORS layer instead of reaching the app.
/// </summary>
public static class E2EHelpers
{
    public const string AccessKey = "fsh.admin.accessToken";
    public const string RefreshKey = "fsh.admin.refreshToken";
    public const string TenantKey = "fsh.admin.tenant";
    public const string PermissionsKey = "fsh.admin.permissions";

    /// <summary>Permissions the seeded root operator holds (covers every page under test).</summary>
    public static readonly string[] AdminPerms =
    [
        "Permissions.Tenants.View",
        "Permissions.Tenants.Create",
        "Permissions.Tenants.Update",
        "Permissions.Users.View",
        "Permissions.Users.Create",
    ];

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private static readonly Dictionary<string, string> CorsHeaders = new()
    {
        ["Access-Control-Allow-Origin"] = "*",
        ["Access-Control-Allow-Methods"] = "GET,POST,PUT,PATCH,DELETE,OPTIONS",
        ["Access-Control-Allow-Headers"] = "Content-Type,Authorization,tenant,X-FSH-App",
    };

    // ------------------------------------------------------------------ JWT

    /// <summary>
    /// Builds a structurally valid HS256 JWT (base64url header + payload + fake
    /// signature). The Blazor AuthStateProvider decodes it with
    /// JwtSecurityTokenHandler.ReadJwtToken (no signature validation), so the
    /// exp/iat/nbf claims must be well-formed — expiry is what gates the session.
    /// </summary>
    public static string FakeJwt(
        string sub,
        string email,
        string name,
        string tenant,
        int expiresInSeconds = 3600)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT",
        };
        var payload = new Dictionary<string, object>
        {
            ["sub"] = sub,
            ["email"] = email,
            ["name"] = name,
            ["tenant"] = tenant,
            ["role"] = "SuperAdmin",
            ["iat"] = now,
            ["nbf"] = now,
            ["exp"] = now + expiresInSeconds,
        };

        return $"{B64Url(JsonSerializer.Serialize(header))}.{B64Url(JsonSerializer.Serialize(payload))}.sig";
    }

    private static string B64Url(string json)
        => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    /// <summary>The shape the login endpoint returns (AuthService reads AccessToken/RefreshToken).</summary>
    public static object LoginTokenResponse(string email = "admin@root.com", string tenant = "root")
        => new
        {
            accessToken = FakeJwt("u-root-1", email, "Root Admin", tenant),
            refreshToken = "fake-refresh-token",
            accessTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            refreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
        };

    // ------------------------------------------------- authed-session seeding

    /// <summary>
    /// Seeds the token/permission localStorage keys via an init script, exactly
    /// like React's seedAuthedSession. The app's AdminTokenStore reads the same
    /// keys (fsh.admin.accessToken / refreshToken / tenant / permissions) and
    /// PermissionsProvider hydrates from localStorage before any API call, so a
    /// seeded session is fully authenticated without a backend.
    ///
    /// The returned registration must be DISPOSED once the first page load is
    /// done: init scripts re-run on every full reload, and the app force-loads
    /// on unauthenticated redirects (RedirectToLogin) and cross-tab logout —
    /// a live registration would silently re-authenticate the session.
    /// </summary>
    public static Task<IAsyncDisposable> SeedAuthedSessionAsync(
        IBrowserContext context,
        string sub = "u-root-1",
        string email = "admin@root.com",
        string name = "Root Admin",
        string tenant = "root",
        string[]? permissions = null)
        => context.AddInitScriptAsync(SeedScript(SeedArg(sub, email, name, tenant, permissions)));

    public static Task<IAsyncDisposable> SeedAuthedSessionAsync(
        IPage page,
        string sub = "u-root-1",
        string email = "admin@root.com",
        string name = "Root Admin",
        string tenant = "root",
        string[]? permissions = null)
        => page.AddInitScriptAsync(SeedScript(SeedArg(sub, email, name, tenant, permissions)));

    private const string SeedScriptTemplate =
        "const a = {0};" +
        "localStorage.setItem(a.accessKey, a.access);" +
        "localStorage.setItem(a.refreshKey, 'fake-refresh-token');" +
        "localStorage.setItem(a.tenantKey, a.tenant);" +
        "localStorage.setItem(a.permissionsKey, JSON.stringify(a.permissions));";

    private static string SeedScript(string json) => string.Format(SeedScriptTemplate, json);

    private static string SeedArg(string sub, string email, string name, string tenant, string[]? permissions)
        => JsonSerializer.Serialize(new
        {
            accessKey = AccessKey,
            access = FakeJwt(sub, email, name, tenant),
            refreshKey = RefreshKey,
            tenantKey = TenantKey,
            tenant,
            permissionsKey = PermissionsKey,
            permissions = permissions ?? AdminPerms,
        }, JsonOpts);

    // ---------------------------------------------------------- route mocks

    /// <summary>Registers a route mock serving <paramref name="body"/> as JSON (last-registered wins).</summary>
    public static Task MockJsonAsync(IPage page, string urlGlob, object body, int status = 200)
        => page.RouteAsync(urlGlob, route => RouteJsonAsync(route, body, status));

    /// <summary>Route handler that answers OPTIONS preflights and otherwise serves JSON.</summary>
    public static async Task RouteJsonAsync(IRoute route, object body, int status = 200)
    {
        if (route.Request.Method == "OPTIONS")
        {
            await FulfillPreflightAsync(route);
            return;
        }

        await route.FulfillAsync(new RouteFulfillOptions
        {
            Status = status,
            ContentType = "application/json",
            Body = JsonSerializer.Serialize(body, JsonOpts),
            Headers = CorsHeaders,
        });
    }

    public static async Task RouteProblemAsync(IRoute route, int status, string title, string detail)
    {
        if (route.Request.Method == "OPTIONS")
        {
            await FulfillPreflightAsync(route);
            return;
        }

        await route.FulfillAsync(new RouteFulfillOptions
        {
            Status = status,
            ContentType = "application/problem+json",
            Body = JsonSerializer.Serialize(new { title, status, detail }, JsonOpts),
            Headers = CorsHeaders,
        });
    }

    private static Task FulfillPreflightAsync(IRoute route)
        => route.FulfillAsync(new RouteFulfillOptions
        {
            Status = 204,
            Headers = CorsHeaders,
        });

    /// <summary>Server-shaped paged response (camelCase, matching PagedResult&lt;T&gt; deserialization).</summary>
    public static object Paged<T>(IEnumerable<T> items, int totalCount, int pageNumber = 1, int pageSize = 12)
    {
        var list = items.ToList();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        return new
        {
            items = list,
            pageNumber,
            pageSize,
            totalCount,
            totalPages,
            hasPrevious = pageNumber > 1,
            hasNext = pageNumber < totalPages,
        };
    }

    // ------------------------------------------------------- shell mocks

    /// <summary>
    /// Mocks every API call the authenticated AppShell fires so any protected
    /// page renders cleanly: a 404 catch-all for anything unmocked (preflight
    /// OPTIONS always answered), SignalR aborts, and the notification bell.
    /// Permissions echo the seeded set so re-hydration after login/logout keeps
    /// the in-memory cache consistent. Broad mocks first — tests register
    /// page-specific mocks AFTER this so they take precedence.
    /// </summary>
    public static async Task InstallShellMocksAsync(IPage page, string[]? permissions = null)
    {
        await page.RouteAsync(
            "**/api/v1/**",
            route => RouteProblemAsync(route, 404, "Not Found", "Unmocked endpoint in E2E test."));

        await page.RouteAsync("**/negotiate**", route => route.AbortAsync());
        await page.RouteAsync("**/api/v1/realtime/**", route => route.AbortAsync());

        await MockJsonAsync(page, "**/api/v1/notifications**", Array.Empty<object>());
        await MockJsonAsync(page, "**/api/v1/notifications/unread-count**", 0);
        await MockJsonAsync(page, "**/api/v1/identity/permissions", permissions ?? AdminPerms);
    }

    /// <summary>Mocks the three queries the Overview page fires on load (login lands on "/").</summary>
    public static Task MockOverviewAsync(IPage page)
    {
        var empty = Array.Empty<object>();
        return Task.WhenAll(
            MockJsonAsync(page, "**/api/v1/tenants/*", Paged(empty, 0, pageSize: 1)),
            MockJsonAsync(page, "**/api/v1/billing/plans?includeInactive=true", empty),
            MockJsonAsync(page, "**/api/v1/billing/invoices*", Paged(empty, 0, pageSize: 50)));
    }

    // -------------------------------------------------------- boot helpers

    /// <summary>Waits for the Blazor WASM runtime to boot and render (MudThemeProvider is the app root).</summary>
    public static async Task WaitForBlazorReadyAsync(IPage page)
    {
        await page.WaitForSelectorAsync(".mud-theme-provider", new PageWaitForSelectorOptions
        {
            Timeout = 90_000,
            State = WaitForSelectorState.Attached,
        });

        var errorUi = page.Locator("#blazor-error-ui");
        if (await errorUi.IsVisibleAsync())
        {
            throw new InvalidOperationException(
                "The Blazor app surfaced #blazor-error-ui during boot (unhandled WASM error). See devserver log.");
        }
    }

    public static async Task<string?> GetLocalStorageAsync(IPage page, string key)
        => await page.EvaluateAsync<string?>(
            "(k) => localStorage.getItem(k)", key);

    /// <summary>
    /// Fills a MudTextField and blurs it with Tab. MudBlazor's @bind-Value only
    /// updates on the browser-native `change` event (blur); Playwright's synthetic
    /// fill events and even real keystrokes do NOT reach Blazor's state, leaving
    /// the form (and submit buttons bound to its fields) stale. Tab after every
    /// fill — this is the pattern that actually enables the bound state.
    /// </summary>
    /// <summary>
    /// Polls until the page URL starts with the given prefix. Deterministic
    /// where WaitForURLAsync is racy (it needs a navigation event, which may
    /// have already committed before the call) and ToHaveURLAsync compares
    /// literally in this Playwright build.
    /// </summary>
    public static async Task WaitForUrlPrefixAsync(IPage page, string prefix, int timeoutMs = 15_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (page.Url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Page URL '{page.Url}' did not start with '{prefix}' within {timeoutMs} ms.");
    }
    public static async Task FillAndBlurAsync(ILocator field, string value)
    {
        await field.FillAsync(value);
        await field.PressAsync("Tab");
        await Task.Delay(100);
    }
}
