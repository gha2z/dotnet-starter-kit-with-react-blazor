# Chapter 0: Foundation & Tooling — "The First Brick"

> **Audience**: From absolute beginner to senior. Each section has tiered callouts.
> **Prerequisites**: A computer with internet access. That's it.
> **Time to complete**: 2-4 hours reading + 1-2 hours hands-on.
> **What you'll know after this chapter**: How a Blazor WASM app boots, how MudBlazor components render, why we have two HttpClients, what IL trimming does, and the exact role of every file in `BlazorShared`.

---

## 0.0 Before You Begin — Environment Setup

This section makes sure your machine can build and run everything we discuss.

### 0.0.1 Install the .NET 10 SDK

```powershell
# Open a terminal (PowerShell 7+, or Windows Terminal) and run:
dotnet --version
```

**✅ Expected**: `10.0.xxx` or higher.

If you don't have .NET 10, download it from [dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0). The "SDK" includes everything: compiler, runtime, CLI tools.

> **🐣 Beginner**: .NET SDK = Software Development Kit. It's the toolbox. `dotnet` is the hammer. `dotnet build` pounds nails (compiles code). `dotnet run` runs the result.

### 0.0.2 Install WASM Tools Workload

Blazor WASM compiles C# into WebAssembly — a binary format browsers can execute. The WASM tooling is an optional .NET workload:

```powershell
dotnet workload list
```

If `wasm-tools` is not listed:

```powershell
dotnet workload install wasm-tools
```

### 0.0.3 Verify Node.js (for React reference apps)

```powershell
node --version
```

**✅ Expected**: `v22.x.x` or higher. Download from [nodejs.org](https://nodejs.org/).

### 0.0.4 IDE Choices

| IDE | Best for | Cost | Notes |
|-----|----------|------|-------|
| **VS Code** + C# Dev Kit | All levels, cross-platform | Free | Install "C# Dev Kit" and "MudBlazor Snippets" extensions |
| **Visual Studio 2022** | Windows-only, heavy projects | Community free | Built-in Blazor tooling, Hot Reload |
| **JetBrains Rider** | Cross-platform, power users | Paid (30-day trial) | Best refactoring, built-in MudBlazor support |

> **🧑‍💻 Junior Tip**: Start with VS Code. It's the lightest, most universal, and teaches you CLI-first workflows that transfer to CI/CD.

### 0.0.5 Clone the Repository

```powershell
git clone <repo-url>
cd dotnet-starter-kit-with-react-blazor-main
```

### 0.0.6 Build Everything to Verify

```powershell
# Build BlazorShared RCL (shared library)
dotnet build clients/BlazorShared/FSH.BlazorShared.csproj

# Build Admin WASM app
dotnet build clients/admin-blazor/FSH.Admin.Wasm/FSH.Admin.Wasm.csproj

# Build Dashboard WASM app
dotnet build clients/dashboard-blazor/FSH.Dashboard.Wasm/FSH.Dashboard.Wasm.csproj
```

**✅ Expected**: Each command prints `Build succeeded. 0 Warning(s) 0 Error(s)`.

---

## 0.1 What Are We Building?

### The Big Picture

This repository is a **modular monolith** backend (C#, .NET 10, PostgreSQL) that ships with **four front-ends**:

| Front-end | Tech | Purpose |
|---|---|---|
| `clients/admin` | React 19 + Vite | Operator/SuperAdmin console |
| `clients/dashboard` | React 19 + Vite | Tenant-facing portal |
| `clients/admin-blazor` | **Blazor WASM** | .NET alternative to admin React |
| `clients/dashboard-blazor` | **Blazor WASM** | .NET alternative to dashboard React |

**Why two front-end stacks?** Because not every team knows React. .NET shops often have C# developers who find Blazor more productive. The goal is **feature parity**: every page, every button, every permission check in the React apps must work identically in the Blazor apps.

### Why Blazor WASM (not Server or Auto)?

| Mode | How it works | Best for |
|------|-------------|----------|
| **Server** | C# runs on server, UI updates sent via SignalR | Intranet, low-latency, small teams |
| **WASM** | C# compiled to WebAssembly, runs in browser | Public-facing SPAs, offline-capable |
| **Auto** | Starts as Server, downloads WASM in background | Hybrid scenarios |

We chose **Standalone WASM** because:

1. **Same architecture as React** — static files deployed to CDN, talks to API via HTTP. No persistent server connection needed.
2. **No SignalR circuit** — Server mode requires a persistent SignalR connection per user. WASM is stateless.
3. **Offline-capable** — The entire app runs in the browser. Once loaded, it can work offline (with PWA).

> **👩‍🔬 Senior Architecture**: WASM has a ~2MB download cost (the .NET runtime). For consumer apps this matters. For B2B SaaS where users stay on the page for hours, the initial load is a one-time cost that amortizes.

### The BlazorShared RCL — Why a Shared Library?

```text
clients/
├── BlazorShared/          ← Razor Class Library (shared code)
│   ├── Auth/              ← ITokenStore, AuthStateProvider
│   ├── Components/        ← FshPageHeader, FshPermissionGate
│   ├── Services/          ← IAuthService, typed API services
│   ├── Models/            ← PagedResult<T>
│   ├── Infrastructure/    ← AuthDelegatingHandler, config
│   ├── Realtime/          ← SignalR hub service
│   ├── Sse/               ← SSE client (dashboard)
│   ├── Permissions/       ← Permission constants
│   └── Theming/           ← FshMudTheme
├── admin-blazor/          ← References BlazorShared
└── dashboard-blazor/      ← References BlazorShared
```

An **RCL** (Razor Class Library) is a .NET project that packages Razor components, C# code, and static assets into a NuGet-like package. Both WASM apps reference it, so they share:

- **Auth infrastructure** — One `ITokenStore` interface, two implementations
- **Components** — `FshPageHeader`, `FshPermissionGate` look identical in both apps
- **Services** — `IAuthService` doesn't care whether it's called from admin or dashboard
- **Theme** — One `MudTheme` definition, two presets (admin green, dashboard rose)

> **🐣 Beginner**: Think of RCL as a "shared folder" that both projects can see. You write code once, use it everywhere. The alternative is copy-paste, which guarantees bugs.

### The Two-HTTP-Client Strategy

This is the single most important architectural decision in Phase 0/1:

```csharp
// Client 1: "FSH.Auth" — NO authentication handler
// Used ONLY for login and token refresh (before we HAVE a token)
builder.Services.AddHttpClient("FSH.Auth", client =>
    client.BaseAddress = new Uri(baseAddress));

// Client 2: Manual HttpClient WITH AuthDelegatingHandler
// Used for EVERYTHING else (authenticated API calls)
builder.Services.AddTransient<AuthDelegatingHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthDelegatingHandler>();
    return new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };
});
```

**Why not one HttpClient?** Because `AuthDelegatingHandler` intercepts 401 responses and tries to refresh the token. But refreshing requires calling the API... with an HttpClient. If that HttpClient also has `AuthDelegatingHandler`, you get infinite recursion: 401 → refresh → 401 → refresh → ...

**The solution**: Two HttpClients. One ("FSH.Auth") is bare — no handler, no auth. It's used ONLY for login and refresh. The other goes through the handler and is used for all authenticated calls.

> **🧑‍💻 Junior Deep Dive**: The `IHttpClientFactory` creates named clients. `CreateClient("FSH.Auth")` returns an HttpClient with the base address and timeout you configured, but no middleware. It's a plain HTTP pipe. The manual `new HttpClient(handler)` creates a client with `AuthDelegatingHandler` as its innermost handler — every request flows through it.

---

## 0.2 The Blazor WASM Boot Sequence

Let's trace what happens from URL enter to UI paint.

### 0.2.1 `index.html` — The Real Entry Point

Most developers think `Program.cs` is the entry point. It's not. The browser loads `index.html` first:

```html
<!-- clients/admin-blazor/FSH.Admin.Wasm/wwwroot/index.html -->
<head>
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <link href="css/app.css" rel="stylesheet" />
</head>
<body>
    <div id="app">
        <!-- Loading state BEFORE Blazor boots -->
        <div class="fsh-loading-container">
            <div class="fsh-loading-spinner"></div>
            <p>Loading...</p>
        </div>
    </div>
    <div id="blazor-error-ui" class="fsh-error-ui">
        <div class="fsh-error-content">
            <p>An unhandled error has occurred.</p>
            <button onclick="window.location.reload()">Reload</button>
        </div>
    </div>
    <script src="_framework/blazor.webassembly.js"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
</body>
```

**The loading sequence**:

1. Browser downloads `index.html` (instant — it's tiny)
2. CSS files load — MudBlazor styles + our custom `app.css`
3. Browser sees `<div id="app">` with the loading spinner inside
4. `blazor.webassembly.js` starts downloading — this is the .NET runtime + compiled WASM
5. While downloading, the user sees the **loading spinner** (pure CSS, no JS needed)
6. .NET runtime boots, Blazor starts, it finds `<div id="app">` and replaces its contents with the rendered `App.razor` component tree
7. `blazor-error-ui` is hidden by default (`display: none`) — it only shows if Blazor crashes

> **🐣 Beginner**: The div content before Blazor loads is called "prerendering" or "loading state". It's crucial because WASM takes 1-5 seconds to download. Without it, the user sees a blank white page and thinks the app is broken.

**File reference**: `clients/admin-blazor/FSH.Admin.Wasm/wwwroot/index.html`

### 0.2.2 `Program.cs` — Service Registration Orchestra

After Blazor boots, it runs `Program.cs`:

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
```

- `RootComponents.Add<App>("#app")` — Mount the `App` component into the `<div id="app">` element
- `RootComponents.Add<HeadOutlet>("head::after")` — Allows Blazor components to modify the `<head>` (title, meta tags)

Then comes service registration. The **order matters**:

```csharp
// 1. Runtime config — needs to be first because services below might depend on it
builder.Services.AddScoped<IRuntimeConfigService, RuntimeConfigService>();

// 2. Auth — must come before HTTP because HTTP depends on ITokenStore
builder.Services.AddSingleton<ITokenStore>(sp =>
    new AdminTokenStore(sp.GetRequiredService<IJSRuntime>()));
builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<AuthStateProvider>());

// 3. HTTP clients — depends on ITokenStore (via AuthDelegatingHandler)
builder.Services.AddHttpClient("FSH.Auth", client => ...);
builder.Services.AddTransient<AuthDelegatingHandler>();

// 4. MudBlazor — independent
builder.Services.AddMudServices(config => ...);
```

**Why this order?** Some services depend on others. `AuthDelegatingHandler` needs `ITokenStore`. If you register it before, the DI container will throw at runtime because it can't resolve the dependency. In .NET DI, registration order between unrelated services doesn't matter, but services must be registered before anything tries to resolve them.

> **👩‍🔬 Senior Architecture**: The `_ = config.LoadAsync()` pattern is "fire and forget" at startup. The config service kicks off an HTTP call to load `/config.json`. It doesn't `await` — the app continues booting while the config loads in the background. If the config fails, defaults are used. This prevents a cold-start race condition where the app waits for a config file that might not exist yet.

**File reference**: `clients/admin-blazor/FSH.Admin.Wasm/Program.cs` (full 59 lines)

### 0.2.3 `App.razor` — The Component Tree Root

```razor
<MudThemeProvider Theme="FshMudTheme.CreateAdmin()" />
<MudDialogProvider />
<MudSnackbarProvider />

<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
                <NotAuthorized>
                    <RedirectToLogin />
                </NotAuthorized>
                <Authorizing>
                    <MudOverlay Visible="true">
                        <MudProgressCircular Indeterminate="true" />
                    </MudOverlay>
                </Authorizing>
            </AuthorizeRouteView>
        </Found>
        <NotFound>
            <MudText>404 — Page not found</MudText>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

This is a **three-layer sandwich**:

| Layer | Purpose | MudBlazor Component |
|-------|---------|-------------------|
| **Theme** | Sets colors, fonts, border radius | `MudThemeProvider` |
| **Overlay** | Dialog + snackbar containers | `MudDialogProvider` + `MudSnackbarProvider` |
| **Auth Shell** | Routes, auth checks, layouts | `CascadingAuthenticationState` → `Router` → `AuthorizeRouteView` |

**The routing state machine**:

```
Browser navigates to /users
    │
    ▼
Router matches route → <Found>
    │
    ▼
AuthorizeRouteView checks if user is authenticated
    │
    ├── ✅ Authenticated → Render the page with MainLayout
    ├── ❌ Not authenticated → <NotAuthorized> → RedirectToLogin
    └── ❓ Still checking → <Authorizing> → MudProgressCircular spinner
```

**The magic**: `CascadingAuthenticationState` provides the authentication state to every component in the tree without passing it as a parameter. Any component can inject `AuthenticationStateProvider` and check the current user.

> **🐣 Beginner**: Think of `CascadingAuthenticationState` as a radio tower. It broadcasts "the user is logged in" on a frequency that every component can tune into. Components don't need to know WHERE the signal comes from — they just check the signal.

**File reference**: `clients/admin-blazor/FSH.Admin.Wasm/App.razor` (26 lines)

### 0.2.4 `MainLayout.razor` — The Visual Shell

```razor
@inherits LayoutComponentBase

<AuthorizeView>
    <Authorized>
        <MudLayout>
            <MudAppBar>...</MudAppBar>
            <MudDrawer>...</MudDrawer>
            <MudMainContent>
                <MudContainer MaxWidth="MaxWidth.ExtraExtraLarge" Class="py-6">
                    @Body
                </MudContainer>
            </MudMainContent>
        </MudLayout>
    </Authorized>
    <NotAuthorized>
        <MudContainer>@Body</MudContainer>
    </NotAuthorized>
</AuthorizeView>
```

The layout has two **visual modes**:

- **Authorized**: Full shell with AppBar (top bar), Drawer (sidebar), and MainContent area. The sidebar shows navigation links (Users, Roles, Tenants, etc.).
- **Not authorized**: Bare container — just the page content (login form) centered on screen. No sidebar, no app bar.

This is why the login page (`/login`) doesn't show the sidebar: it's rendered inside `<NotAuthorized>`, which is just a centered container.

> **🧑‍💻 Junior Deep Dive**: `@Body` is where the routed page renders. `MainLayout` wraps it. Think of `@Body` as a slot — the router fills it with the matched page component. The layout stays constant as you navigate between pages, only `@Body` changes.

**File reference**: `clients/admin-blazor/FSH.Admin.Wasm/Shared/MainLayout.razor`

---

## 0.3 MudBlazor — The Component Library

### 0.3.1 Why MudBlazor (Not Radzen, Not Syncfusion)?

| Library | Cost | Themeable | Mobile-responsive | MAUI-compatible |
|---------|------|-----------|-------------------|---|
| **MudBlazor** | Free (MIT) | ✅ Full MudTheme | ✅ MudBreakpoint | ✅ BlazorWebView |
| Radzen | Free/Paid | Limited | ✅ | Partial |
| Syncfusion | Paid ($995/yr) | Complex | ✅ | ❌ |
| Telerik | Paid ($1,499/yr) | Complex | Partial | ❌ |
| MatBlazor | Free | Basic | ❌ | ❌ |

MudBlazor wins on three criteria:

1. **Material Design 3** — Modern, accessible, mobile-first out of the box
2. **MudTheme system** — Brand identity in 20 lines of C#, no CSS overrides needed
3. **MAUI compatibility** — `MudBlazor` components work inside MAUI `BlazorWebView` without modification

### 0.3.2 The MudBlazor 8 → 9 Migration (What We Fixed)

The existing code was written for MudBlazor 8.5.0. We upgraded to 9.7.0. Here's every breaking change we hit and how we fixed it:

#### 🔴 Breaking Change 1: `IDialogService.ShowMessageBox` Removed

```
⚠️ Error CS1061: 'IDialogService' does not contain a definition for 'ShowMessageBox'
```

MudBlazor 9 removed the static `ShowMessageBox` helper. **Our fix**: A custom `FshConfirmDialog` component.

```csharp
// NEW: FshConfirmDialog.razor
<MudButton OnClick="OpenAsync" Color="Color.Error" StartIcon="@Icons.Material.Filled.Delete">
    Delete
</MudButton>

@code {
    private async Task OpenAsync()
    {
        var parameters = new DialogParameters
        {
            { "Message", "Are you sure?" },
            { "ConfirmText", "Delete" },
            { "CancelText", "Cancel" },
        };
        var dialog = await DialogService.ShowAsync<FshConfirmDialogContent>(
            "Confirm", parameters, options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled)
            await OnConfirmed.InvokeAsync();
    }
}
```

**File reference**: `clients/BlazorShared/Components/FshConfirmDialog.razor` + `FshConfirmDialogContent.razor`

> **🐣 Beginner**: `ShowAsync<FshConfirmDialogContent>` tells MudBlazor: "Render this component as a dialog." The `T` parameter is the component type. MudBlazor creates an instance, passes the parameters, and shows it in a modal overlay.

#### 🔴 Breaking Change 2: `MudDialogInstance` → `IMudDialogInstance`

```
⚠️ Error CS0618: 'MudDialogInstance' is obsolete: 'Use IMudDialogInstance instead'
```

```diff
- [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;
+ [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
```

A simple rename. The interface is the same contract; the concrete class was internalized.

#### 🔴 Breaking Change 3: `Dense` Attribute Removed from `MudTextField`

```
⚠️ Error: The attribute 'Dense' is not recognized by 'MudTextField'
```

```diff
- <MudTextField Dense="true" ... />
+ <MudTextField Margin="Margin.Dense" ... />
```

MudBlazor 9 replaced the `Dense` boolean with the `Margin` enum. `Margin.Dense` produces the same compact layout.

#### ⚠️ Breaking Change 4: `MudForm.Validate()` Obsoleted

```
⚠️ Warning CS0618: 'MudForm.Validate()' is obsolete: 'Use ValidateAsync instead.'
```

```diff
- await _form.Validate();
+ await _form.ValidateAsync();
```

Same behavior, new method. The old one returns `bool` synchronously, which doesn't work with async validators.

#### ⚠️ Breaking Change 5: `MUD0002` Analyzer — `Dismissible` Casing

```
⚠️ Warning MUD0002: Illegal Attribute 'Dismissible' on 'MudAlert' using pattern 'LowerCase'
```

MudBlazor 9 introduced a Roslyn analyzer that enforces attribute naming conventions. The actual attribute name uses a specific pattern. We simply removed the `Dismissible` attribute since MudAlerts are dismissible by default with the right configuration.

### 0.3.3 The Theme System — Brand Without CSS

```csharp
// clients/BlazorShared/Theming/FshMudTheme.cs
public static MudTheme CreateAdmin()
{
    return new MudTheme
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#5B8C5A",       // Sage green
            Secondary = "#4A7B8C",     // Steel blue
            AppbarBackground = "#1B2A2C", // Dark teal
            Background = "#F0F2F5",    // Cool grey
            DrawerBackground = "#F5F6F7",
            // ... 12 more color tokens
        },
        PaletteDark = new PaletteDark { ... },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "6px",
            DrawerWidthLeft = "260px",
        },
    };
}
```

**What's happening here?** MudBlazor exposes every visual property as C# objects. You don't write CSS variables or Sass files. You set `Primary` and MudBlazor generates the Material Design 3 color system from it (lighter, darker, contrast, surface variants).

**Admin vs Dashboard**: `CreateDashboard()` calls `CreateAdmin()` and overrides only the primary color:

```csharp
public static MudTheme CreateDashboard()
{
    var theme = CreateAdmin();
    theme.PaletteLight.Primary = "#E85D75"; // Rose
    theme.PaletteDark.Primary = "#F07A8E";
    return theme;
}
```

This is the **template method pattern**: define the 90% common theme, let each app override the 10% that makes it unique.

> **🐣 Beginner**: You might think "why C# for colors instead of CSS?" Because MudBlazor uses these values to calculate 20+ derived colors (hover, focus, disabled, surface variants). Writing them in CSS would be 500 lines of variables. 20 lines of C# achieves the same.

**File reference**: `clients/BlazorShared/Theming/FshMudTheme.cs` (full 75 lines)

---

## 0.4 The Auth Infrastructure

### 0.4.1 ITokenStore — The localStorage Bridge

**The problem**: Blazor WASM runs C# in the browser. `localStorage` is a JavaScript API. We need a way for C# to read/write browser storage.

**The bridge**: `IJSRuntime` — Blazor's built-in JS interop mechanism.

```csharp
// clients/admin-blazor/FSH.Admin.Wasm/Auth/AdminTokenStore.cs
public sealed class AdminTokenStore(IJSRuntime js) : ITokenStore
{
    private const string Prefix = "fsh.admin";
    private const string AccessKey = $"{Prefix}.accessToken";

    public async Task<string?> GetAccessTokenAsync()
        => await js.InvokeAsync<string?>("localStorage.getItem", AccessKey);

    public async Task SetTokensAsync(string accessToken, string? refreshToken)
    {
        await js.InvokeVoidAsync("localStorage.setItem", AccessKey, accessToken);
        await js.InvokeVoidAsync("localStorage.setItem", RefreshKey, refreshToken);
        TokensChanged?.Invoke();
    }
}
```

**How it works**: `js.InvokeAsync<string?>("localStorage.getItem", "fsh.admin.accessToken")` translates to:

```javascript
// What Blazor generates:
localStorage.getItem("fsh.admin.accessToken")
```

The first parameter is the **JavaScript function identifier**. The remaining parameters are **arguments**. Blazor marshals them across the WASM-JS boundary (serialization, type conversion, and transfer).

> **🐣 Beginner**: Think of `IJSRuntime` as a phone line to JavaScript. C# dials `localStorage.getItem` and the JS operator answers with the result. The phone line has a protocol: arguments are serialized to JSON, passed to JS, and the return value is deserialized back to C#.

**Namespace isolation**: Admin uses `fsh.admin.*`, dashboard uses `fsh.dashboard.*`. This prevents token collision when both apps run in the same browser (during development).

```
localStorage:
  fsh.admin.accessToken = "eyJ..."  ← Admin app
  fsh.dashboard.accessToken = "eyJ..."  ← Dashboard app
```

> **🧑‍💻 Junior Deep Dive**: Why not `ProtectedBrowserStorage`? That's an abstraction built on top of the same `IJSRuntime` pattern but with data protection (encryption). It adds serialization overhead and uses different key names. We chose direct JS interop because it matches the React codebase's exact key names (`fsh.*`), making cross-referencing trivial.

**File reference**: `clients/BlazorShared/Auth/ITokenStore.cs` (interface) + `clients/admin-blazor/FSH.Admin.Wasm/Auth/AdminTokenStore.cs` (implementation)

### 0.4.2 AuthStateProvider — The JWT Decoder

Blazor's `AuthenticationStateProvider` is the central auth authority. Every component that uses `<AuthorizeView>` or `[Authorize]` ultimately queries this service.

```csharp
// clients/BlazorShared/Auth/AuthStateProvider.cs
public sealed class AuthStateProvider(ITokenStore tokenStore) : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokenStore.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token) || IsTokenExpired(token))
            return AnonymousState;

        var principal = CreatePrincipal(token);
        return new AuthenticationState(principal);
    }

    public async Task NotifyLoginAsync(string accessToken, string? refreshToken, string tenant)
    {
        await tokenStore.SetTokensAsync(accessToken, refreshToken);
        await tokenStore.SetTenantAsync(tenant);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static bool IsTokenExpired(string token)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        return jwt.ValidTo <= DateTime.UtcNow.AddSeconds(10);
    }
}
```

**The boot sequence**:

1. User opens app (or refreshes page)
2. `Router` asks `AuthorizeRouteView`: "Can this user see /users?"
3. `AuthorizeRouteView` calls `AuthenticationStateProvider.GetAuthenticationStateAsync()`
4. `AuthStateProvider` reads `localStorage` via `ITokenStore`
5. If token exists and is not expired → creates a `ClaimsPrincipal` from JWT claims
6. If token missing or expired → returns anonymous identity
7. `AuthorizeRouteView` renders either the page or `<NotAuthorized>`

**The JWT decode**: `JwtSecurityTokenHandler().ReadJwtToken(token)` parses the JWT without validating the signature (client-side validation is pointless — the server validates). It extracts claims like `sub` (user ID), `email`, `name`, `tenant`, `permissions`, and builds a `ClaimsIdentity`.

**The 10-second skew**: `jwt.ValidTo <= DateTime.UtcNow.AddSeconds(10)` — tokens that expire within 10 seconds are treated as already expired. This "clock skew" window accounts for differences between the server's clock and the browser's clock.

> **👩‍🔬 Senior Architecture**: The `AnonymousState` is cached as a `static readonly Task` — it's created once and reused. This avoids allocating a new `AuthenticationState` object every time an unauthenticated user navigates. Micro-optimization, but in Blazor WASM where memory is constrained, every allocation matters.

**File reference**: `clients/BlazorShared/Auth/AuthStateProvider.cs` (52 lines)

### 0.4.3 AuthDelegatingHandler — The 401 Interceptor

This is an `HttpMessageHandler` — a piece of middleware in the HTTP pipeline. Every HTTP request passes through it:

```csharp
public sealed class AuthDelegatingHandler(
    ITokenStore tokenStore,
    IHttpClientFactory httpClientFactory) : DelegatingHandler
{
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        // Step 1: Attach Authorization header
        var token = await tokenStore.GetAccessTokenAsync();
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Step 2: Attach tenant header
        var tenant = await tokenStore.GetTenantAsync();
        if (tenant is not null)
            request.Headers.TryAddWithoutValidation("tenant", tenant);

        // Step 3: Send the request
        var response = await base.SendAsync(request, ct);

        // Step 4: If 401, try to refresh the token
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var refresh = await tokenStore.GetRefreshTokenAsync();
            if (refresh is not null)
            {
                await RefreshLock.WaitAsync(ct);
                try
                {
                    var newToken = await RefreshAccessTokenAsync(
                        token!, refresh, ct);
                    await tokenStore.SetTokensAsync(
                        newToken.Token, newToken.RefreshToken);
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", newToken.Token);
                    response.Dispose();
                    response = await base.SendAsync(request, ct); // Retry
                }
                catch
                {
                    await tokenStore.ClearAsync();
                    throw new ApiRequestException(401, "Session expired");
                }
                finally { RefreshLock.Release(); }
            }
        }
        return response;
    }
}
```

**The single-flight refresh algorithm** (`SemaphoreSlim(1,1)`):

```
Time → Request 1 ──▶ 401 ──▶ Acquire lock ──▶ Refresh ──▶ Retry ──▶ ✅
       Request 2 ──▶ 401 ──▶ ░░ WAIT ░░░░░░░░ ░░ WAIT ░░ ──▶ Use new token ──▶ ✅
       Request 3 ──▶ 401 ──▶ ░░ WAIT ░░░░░░░░ ░░ WAIT ░░ ──▶ Use new token ──▶ ✅
```

Without the lock, three concurrent 401s would trigger **three refresh calls**. The API might rate-limit or invalidate the first refresh token on the second call. `SemaphoreSlim(1,1)` ensures only **one** refresh happens; the other two requests wait, get the new token from the store, and retry with it.

> **🐣 Beginner**: `SemaphoreSlim(1,1)` is a "one-at-a-time" gate. Imagine a single bathroom key. Three people need to use it. The first one takes the key, the other two form a line. When the first finishes, they hand the (now refreshed) key to the next person.

**The refresh API contract**:

```csharp
// Request
POST /api/v1/identity/token/refresh
{ "token": "eyJ...", "refreshToken": "abc123" }

// Response
{ "token": "eyJ...", "refreshToken": "def456" }
```

**⚠️ Common Pitfall**: The response key is `token`, not `accessToken`. The React codebase uses `{ token, refreshToken }`. If your DTO expects `{ accessToken, refreshToken }`, the JSON deserialization silently fails and `accessToken` is null. This was the exact bug we fixed in Phase 1.

**File reference**: `clients/BlazorShared/Infrastructure/AuthDelegatingHandler.cs`

### 0.4.4 Cross-Tab Logout — The Storage Event

When a user logs out in one browser tab, all other tabs should redirect to login.

```csharp
// Admin App.razor.cs
private const string CrossTabLogoutScript = @"
    window.addEventListener('storage', function(e) {
        if (e.key && e.key.startsWith('fsh.admin.') && e.newValue === null) {
            window.location.reload();
        }
    });
";
```

**The `storage` event**: The browser fires a `storage` event on ALL tabs (except the one that made the change) whenever `localStorage` is modified. We listen for keys starting with `fsh.admin.` being removed (`e.newValue === null`) — that's the logout signal.

**Why guarded with try/catch**:

```csharp
try { await Js.InvokeVoidAsync("eval", CrossTabLogoutScript); }
catch { /* CSP may block eval */ }
```

Content Security Policies (CSP) on some deployments may block `eval()`. If so, cross-tab logout silently degrades. The user only stays logged in on the other tab until they refresh the page — acceptable.

**File reference**: `clients/admin-blazor/FSH.Admin.Wasm/App.razor.cs`

---

## 0.5 DI in Blazor WASM — The Bug That Bites

### The `ISseService` Lifetime Mismatch (Real Bug We Fixed)

**The bug report**: Dashboard app crashed at startup with:

```
System.InvalidOperationException: Cannot consume scoped service
'System.Net.Http.HttpClient' from singleton 'FSH.BlazorShared.Sse.SseService'
```

**The problem**:

```csharp
// ❌ BAD: Singleton depends on Scoped
builder.Services.AddSingleton<ISseService, SseService>();
// SseService constructor: SseService(HttpClient http, ...)
// HttpClient is registered as Scoped in WASM
```

In Blazor WASM, `HttpClient` from `AddHttpClient` is registered as **Scoped**. A Singleton can only depend on other Singletons or static data. Injecting a Scoped service into a Singleton is a circular lifetime violation — the container doesn't know which scope's HttpClient to give.

**The fix**:

```csharp
// ✅ GOOD: Scoped depends on Scoped
builder.Services.AddScoped<ISseService, SseService>();
```

**Why this works in WASM but not Server**:

| Runtime | Scoped behavior | 
|---------|----------------|
| **Blazor Server** | One scope per connection/circuit. Singleton across all users. |
| **Blazor WASM** | One scope per app instance. Singleton = Scoped (there's only one user per WASM instance). |

In WASM, `AddScoped` behaves like `AddSingleton` because the app runs in a single-user browser tab. But the DI container doesn't know that — it enforces the rules strictly. Using `AddScoped` satisfies the checker and works correctly.

> **👩‍🔬 Senior Architecture**: This is a **container-agnostic design** lesson. If you write `AddSingleton<ISseService>` and later port to Blazor Server, it breaks. `AddScoped` works in both WASM and Server. Rule: **Default to Scoped** unless you know for certain the service is stateless and can be safely shared.

**How to audit**: Search for `.AddSingleton<` in `Program.cs`. Every singleton should be justified with a comment:

```csharp
builder.Services.AddSingleton<ITokenStore>(sp => ...);
// ✅ Singleton: TokenStore is just a IJSRuntime wrapper — no scoped dependencies
```

---

## 0.6 IL Trimming — Why We Turned It Off

### What IL Trimming Does

```xml
<!-- WASM .csproj -->
<BlazorWebAssemblyEnableTrimming>false</BlazorWebAssemblyEnableTrimming>
```

When `true`, the .NET linker analyzes your code at publish time and removes any IL (Intermediate Language) code that it thinks is unused.

**The goal**: Smaller WASM bundles. The .NET runtime DLLs are ~2MB. Trimming can reduce that to ~1MB.

**The problem**: The linker uses **static analysis** (tracing method calls). It can't see:

1. **Reflection**: `JsonSerializer.Deserialize<T>(json)` — T is determined at runtime
2. **Dynamic loading**: `Activator.CreateInstance(typeName)` — typeName is a string
3. **MudBlazor components**: MudBlazor uses reflection for component discovery and property binding

When trimming removes a type that's needed by reflection, you get:

```
⚠️ Runtime: MissingMethodException — Method 'X' not found
```

This error only appears at runtime, not at compile time. It's a silent killer.

**Our approach**: `false` for development (correctness > speed). In Phase 6, we'll add a `Linker.xml` that explicitly preserves the types MudBlazor and JSON serialization need.

> **🧑‍💻 Junior Deep Dive**: A `Linker.xml` file tells the linker: "Don't remove these types, even if static analysis says they're unused." It's an allowlist. You add it to your project and the linker reads it before pruning.

---

## 0.7 The `app.css` — Loading States That Don't Suck

Blazor WASM downloads a 2-3MB .NET runtime before it can render anything. During that download, the user sees the HTML that's in `<div id="app">` — the **loading state**.

```css
/* clients/admin-blazor/FSH.Admin.Wasm/wwwroot/css/app.css */
.fsh-loading-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    min-height: 100vh;
}

.fsh-loading-spinner {
    width: 40px;
    height: 40px;
    border: 3px solid #D0D5DD;
    border-top-color: #5B8C5A;
    border-radius: 50%;
    animation: fsh-spin 0.8s linear infinite;
}
```

**The spinner animation**:

```css
@keyframes fsh-spin {
    to { transform: rotate(360deg); }
}
```

This is a **pure CSS spinner**. No JavaScript, no images, no external dependencies. It works as soon as the CSS file loads (which is before the WASM runtime even starts downloading).

**The error bar**:

```css
.fsh-error-ui {
    display: none; /* Hidden until Blazor crashes */
}
```

Blazor's boot script (`blazor.webassembly.js`) looks for an element with `id="blazor-error-ui"`. If Blazor fails to boot, it sets `display: block` on this element, showing the error message and reload button. This is built-in Blazor behavior — you just need to provide the HTML structure.

---

## 0.8 JS Interop — The Bridge Between Two Worlds

### The Core Pattern

```csharp
// C# calling JavaScript
await js.InvokeAsync<ReturnType>("jsFunctionName", arg1, arg2);
await js.InvokeVoidAsync("jsFunctionName", arg1, arg2); // When no return value
```

### Three JS Interop Uses in Phase 0

| Use | JS Function | C# Call | Why Not Pure C#? |
|-----|-------------|---------|-----------------|
| localStorage | `localStorage.getItem(key)` | `js.InvokeAsync<string?>(...)` | localStorage is a browser API, not available in .NET |
| Cross-tab logout | `window.addEventListener('storage', ...)` | `js.InvokeVoidAsync("eval", script)` | The `storage` event is browser-only |
| SSE (dashboard) | `EventSource` (via polyfill) | JS interop | Blazor's HttpClient can't stream SSE responses |

### The try/catch Guard

Every JS call is wrapped in try/catch:

```csharp
try { await Js.InvokeVoidAsync("eval", script); }
catch { /* graceful degradation */ }
```

**Why?** Three reasons:

1. **CSP blocks `eval`** — Some security policies prohibit `eval()`. The script silently fails.
2. **Browser compatibility** — `storage` event works in all modern browsers, but if it doesn't, we degrade.
3. **Blazor reconnection** — In rare cases, the JS interop bridge isn't ready. The try/catch prevents a white screen.

> **🐣 Beginner**: `IJSRuntime` is always available in Blazor WASM (it's the WASM-JS bridge). But the JS functions you call might not exist or might be blocked. Always guard your JS interop calls.

---

## 0.9 The Login Stub — Deliberately Minimal

In Phase 0, the login page is intentionally a stub:

```razor
@page "/login"
<div class="d-flex align-center justify-center" style="min-height: 100vh;">
    <MudPaper Class="pa-8" Style="width: 400px;" Elevation="2">
        <MudText Typo="Typo.h5" Align="Align.Center">FullStackHero Admin</MudText>
        <MudText Typo="Typo.body2" Align="Align.Center">Login page coming soon.</MudText>
    </MudPaper>
</div>
```

**Why a stub?** Because `RedirectToLogin` navigates to `/login`. If there's no page at `/login`, the router shows "404 — Page not found". The stub ensures there's a target page.

**Why not the full login in Phase 0?** Because Phase 0's goal is: "Can the app load without crashing?" The full login form depends on `IAuthService`, `AuthStateProvider`, and the token store — those are Phase 1's responsibility.

This is **vertical slicing** in action:
- Phase 0: App loads, routes work, loading states show, error states hide
- Phase 1: User authenticates, tokens store, permissions load
- Each phase delivers a **working end-to-end capability**, even if limited

> **👩‍🔬 Senior Architecture**: Resist the urge to build "just the login form" in Phase 0. It seems small, but it's a wormhole: you need error handling, loading states, token storage, API error mapping... that's Phase 1's scope. Phase 0 is for the skeleton. Phase 1 is for the organs.

**File reference**: `clients/admin-blazor/FSH.Admin.Wasm/Pages/Auth/LoginPage.razor` (15 lines, Phase 0 version)

---

## 0.10 The Configuration Puzzle

### Runtime Config (`/config.json`)

```json
{
  "apiBase": "",
  "defaultTenant": "root",
  "dashboardUrl": "http://localhost:5174"
}
```

This file is loaded at runtime by `RuntimeConfigService`:

```csharp
public async Task LoadAsync(CancellationToken ct = default)
{
    var config = await http.GetFromJsonAsync<ConfigDto>("/config.json", ct);
    if (config is not null)
    {
        ApiBaseUrl = config.ApiBaseUrl ?? "/";
        DefaultTenant = config.DefaultTenant ?? "root";
    }
}
```

**Why not `appsettings.json`?** In Blazor WASM, `appsettings.json` is embedded at build time. `/config.json` is fetched at runtime. This means you can change config after deployment without rebuilding. The React apps use the exact same pattern.

**Why fire-and-forget on load?**

```csharp
_ = config.LoadAsync(); // Don't await — app boots without config
```

If config loading fails (network, missing file, parse error), the app still boots. Services that depend on config values use the defaults. This prevents a cold-start cascade failure where config → HTTP failure → app crash.

---

## 0.11 The `_Imports.razor` Convention

Every Blazor project has a `_Imports.razor` file. It's a global `using` statement for all `.razor` files in that project:

```razor
@using MudBlazor
@using FSH.BlazorShared.Auth
@using FSH.BlazorShared.Components
```

Without `_Imports.razor`, every `.razor` file would need:

```razor
@using MudBlazor
@using FSH.BlazorShared.Auth
@using FSH.BlazorShared.Components
```

`_Imports.razor` applies to ALL `.razor` files in the project (and subdirectories). It's the Blazor equivalent of a `global using` in C#.

**File reference**: `clients/BlazorShared/_Imports.razor`, `clients/admin-blazor/FSH.Admin.Wasm/_Imports.razor`

---

## 0.12 Common Pitfalls & War Stories

### Beginner: Single Quotes in Razor Lambdas

```razor
<!-- ❌ BROKEN: C# sees '/login' as a 6-character char literal -->
<MudButton OnClick="() => Nav.NavigateTo('/login')" />

<!-- ✅ FIXED: Use @() to switch to C# context -->
<MudButton OnClick="@(() => Nav.NavigateTo("/login"))" />
```

**The error**: `CS1012: Too many characters in character literal`

**Why it breaks**: In Razor, attribute values are parsed as C# expressions. `'/login'` is parsed as a character literal starting with `'` and ending with `'`. But `'login'` is 6 characters — too many for `char`.

**The fix**: `@(() => ... )` tells the Razor compiler "the expression starts here" (the `@`), and inside the parentheses, standard C# quoting rules apply with `"`.

### Beginner: MudForm.Validate() → ValidateAsync()

```
⚠️ CS0618: 'MudForm.Validate()' is obsolete: 'Use ValidateAsync instead.'
```

Always use the `Async` suffix. Blazor component methods that involve UI updates should be async to avoid blocking the render thread.

### Junior: MudDialogInstance → IMudDialogInstance

```
⚠️ CS0618: 'MudDialogInstance' is obsolete: 'Use IMudDialogInstance instead'
```

```diff
- [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;
+ [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
```

MudBlazor 9 internalized the concrete type. The `IMudDialogInstance` interface provides the same `Close()` / `Cancel()` methods.

### Junior: Dense Removed from MudTextField

```diff
- <MudTextField Dense="true" />
+ <MudTextField Margin="Margin.Dense" />
```

MudBlazor 9 moved all compact-spacing overrides to the `Margin` enum. `Margin.Dense` is the new `Dense="true"`.

### Senior: Refresh Endpoint Response Contract Mismatch

```
API returns:  { "token": "eyJ...", "refreshToken": "abc" }
Client expects: { "accessToken": "eyJ...", "refreshToken": "abc" }
Result: accessToken is null → any authenticated call fails with 401
```

**The fix**: Match the API contract exactly. Read the React codebase's refresh handler to confirm the response shape:

```typescript
// React source of truth (api-client.ts):
const tokens = await response.json(); // { token, refreshToken }
tokenStore.setTokens(tokens.token, tokens.refreshToken);
```

```csharp
// C# DTO (must mirror React):
private sealed record RefreshResponse(string Token, string? RefreshToken);
// Not: record RefreshResponse(string AccessToken, string? RefreshToken);
```

**The lesson**: When replicating a front-end, **read the existing front-end's source**. Don't guess the API contract. The React codebase IS the API specification.

### Senior: AuthDelegatingHandler Refresh HttpClient URL

The handler creates an HttpClient for the refresh call. This HttpClient has no base address. A relative URL like `/api/v1/identity/token/refresh` resolves to `http://localhost/api/v1/...` which is wrong in development.

**The fix**: Use `IHttpClientFactory` with a named client ("FSH.Auth") that has the correct base address configured in `Program.cs`. This named client is available to both `AuthService` and `AuthDelegatingHandler`.

---

## 0.13 Free Learning Resources

| Topic | What to search | Why |
|-------|---------------|-----|
| **Blazor WASM** | Microsoft Learn: "Build a Blazor WebAssembly app" | Official Microsoft tutorial, step-by-step |
| **MudBlazor** | "MudBlazor docs" + "MudBlazor getting started" | Official docs with all components documented |
| **C# 13** | "What's new in C# 13" Microsoft Docs | Latest C# features used in this codebase |
| **DI in .NET** | "Dependency injection in .NET" Microsoft Docs | MS official guide with Blazor-specific sections |
| **HttpClient factory** | Steve Gordon, "HttpClientFactory in .NET" | The definitive guide on named/typed clients |
| **JWT** | "JWT.io" + "Introduction to JSON Web Tokens" | The interactive JWT debugger at jwt.io |
| **SemaphoreSlim** | "SemaphoreSlim in C#" by Nick Chapsas (YouTube) | Visual explanation of the single-flight pattern |
| **IL Trimming** | "Blazor WASM trimming" Microsoft Docs | Official guide to configuring Linker.xml |
| **Blazor JS interop** | Chris Sainty, "Blazor: A Beginner's Guide" | The best practical book on Blazor patterns |
| **Blazor lifecycle** | "ASP.NET Core Blazor lifecycle" Microsoft Docs | `OnInitializedAsync`, `OnParametersSet`, `Dispose` |
| **WebAssembly** | "WebAssembly concepts" MDN | Understand what WASM is and how it works |
| **.NET Aspire** | "Azure .NET Aspire documentation" Microsoft | Orchestration framework used to run the full stack |
| **Playwright** | "Playwright for .NET" Microsoft Docs | E2E testing framework used in later phases |
| **bUnit** | "bUnit — Blazor component testing" bunit.dev | Unit testing Blazor components |

---

### YouTube Channels Worth Subscribing To

| Channel | Focus | Best for |
|---------|-------|----------|
| **Nick Chapsas** | .NET deep dives, performance | All levels |
| **Claudio Bernasconi** | Blazor-specific tutorials | Junior to mid |
| **Steve Gordon** | HttpClient, resilience, DI | Mid to senior |
| **dotNET** (Microsoft) | Official .NET Conf recordings | All levels |
| **IAmTimCorey** | .NET fundamentals for beginners | Beginner |
| **Raw Coding** | Blazor + MudBlazor project builds | Junior to mid |

---

## 0.14 Chapter 0 Quiz

Test yourself (answers at the bottom):

**Q1**: Why does Blazor WASM need `IJSRuntime` to access `localStorage`?

**Q2**: What happens when `BlazorWebAssemblyEnableTrimming=true` and a type used by `JsonSerializer.Deserialize<T>()` is removed?

**Q3**: Why do we register `ISseService` as `AddScoped` instead of `AddSingleton`?

**Q4**: How does `AuthDelegatingHandler` prevent multiple concurrent token refresh calls?

**Q5**: What's the difference between the `FSH.Auth` HttpClient and the manually-created HttpClient in `Program.cs`?

**Q6**: Why is the login page in Phase 0 just a stub?

**Q7**: What does `_ = config.LoadAsync()` accomplish? Why not `await config.LoadAsync()`?

**Q8**: How does cross-tab logout work?

**Q9**: What file does the browser load first when you navigate to a Blazor WASM app?

**Q10**: Why do admin and dashboard apps use different localStorage prefixes (`fsh.admin.*` vs `fsh.dashboard.*`)?

<details>
<summary>🔽 Click for answers</summary>

**A1**: `localStorage` is a browser API — JavaScript runs in the browser, C# runs in WebAssembly. `IJSRuntime` bridges the two.

**A2**: The type gets removed at publish time. At runtime, `Deserialize<T>()` throws `MissingMethodException` because the type's code is gone. This error only appears at runtime, never at compile time.

**A3**: Because `SseService` depends on `HttpClient`, which is registered as Scoped. A Singleton can't depend on a Scoped service — the DI container throws at startup.

**A4**: It uses `SemaphoreSlim(1,1)`. When the first 401 triggers a refresh, other concurrent 401s wait on the semaphore. After the refresh completes, they reuse the new token.

**A5**: `FSH.Auth` is a bare HttpClient (no middleware). The manual HttpClient has `AuthDelegatingHandler` in its pipeline, which attaches auth headers and handles 401s.

**A6**: Because Phase 0's scope is "app loads without crashing." The full login form belongs to Phase 1. The stub ensures `RedirectToLogin` has a target route.

**A7**: It fires off the config request without waiting. The app continues booting in parallel. If config fails, defaults are used. This prevents a cascade failure.

**A8**: When one tab clears localStorage (logout), the browser fires a `storage` event on all other tabs. `App.razor.cs` listens for keys starting with `fsh.*` being removed, and reloads the page.

**A9**: `index.html`. Blazor WASM is a single-page app — `index.html` is the only HTML file. The `.wasm` runtime and DLLs are loaded by `blazor.webassembly.js`.

**A10**: To prevent token collision when both apps run in the same browser (common during development). Without the prefix, the admin app's token would overwrite the dashboard's.

</details>

---

## 0.15 Next → Chapter 1: Identity & Authentication

Phase 0 built the **skeleton** — the app loads, boots, renders MudBlazor, and shows a login stub. The services are registered, the DI container is configured, and the HTTP pipeline is ready.

Phase 1 adds the **organs** — the full login form, token storage, authentication state, permission loading, and the forgot/reset/confirm email flows. Every piece of auth infrastructure in this chapter gets exercised end-to-end.

**The logical chain**: Without Phase 0's `RedirectToLogin` redirect and login page stub, Phase 1's login form would have nowhere to navigate to. Without Phase 0's `AuthDelegatingHandler`, Phase 1's authenticated API calls would fail. Without Phase 0's MudBlazor theme, Phase 1's login page would be unstyled.

**Continue to**: `opencode/addBlazorFrontends/Phase-01-Identity-And-Auth/hands-on-phase-1.md`

---

*End of Chapter 0. Total: ~8,500 words. Reading time: ~40 minutes.*
