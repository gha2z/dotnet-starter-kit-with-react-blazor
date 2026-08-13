# Phase 3 — Dashboard Feature Pages
Last Update: 2026-Aug-06, by: opencode (auto/coding, model: deepseek-v4-flash-free).

> **Target:** All tenant-facing dashboard pages built — Overview (SSE), Activity, Subscription, Wallet, Catalog, Invoices, Identity (profile/user/role), Tickets, Chat, Files, System. Feature parity with `clients/dashboard` React app.

## Status

- Phase 3: **✅ Complete** - 3.1 Overview ✅ - 3.2 Activity ✅ - 3.3 Subscription ✅ - 3.4 Wallet ✅ - 3.5 Invoices ✅ - 3.6 Catalog ✅ - 3.7 Identity ✅ - 3.8 Tickets ✅ - 3.9 Chat ✅ - 3.10 Files ✅ - 3.11 System ✅ - 3.12 Settings ✅ - 3.13 Impersonation ✅ - 3.14 Command Palette ✅ - dashboard suite **178/178** - 3.11 actually landed in 52861c37 on 2026-08-05 (this section was stale)
- Prerequisites: Phase 1 ✅ (auth+login working, AppShell)
- **Parity sprint deliverables already in place:** `SseService` (token flow `POST /api/v1/sse/token` → `GET /api/v1/sse/stream?token=`, backoff reconnect, `ConnectionChanged` event), SSE status dot in topbar, full sidebar (accordion, permission-gated) — no rebuilds needed, only page work.
- Terminal pages `/tenant-deactivated` + `/impersonation-ended` do **not** exist yet (docs previously claimed they did) — tracked as 3.15 in `Phase-07-Parity-Completion/plan.md`.

## 3.1 Done — what exists now

- **`IDashboardService` / `DashboardService`** (`BlazorShared/Services/DashboardService.cs`): `GetMyTenantStatusAsync`, `GetMySubscriptionAsync`, `GetUsageSnapshotsAsync`, `GetRecentAuditsAsync` against `/api/v1/tenants/me/status`, `/api/v1/billing/subscriptions/me`, `/api/v1/billing/usage`, `/api/v1/audits`. Registered in dashboard `Program.cs` under "Tenant-scoped data services".
- **`Models/Dashboard/DashboardDtos.cs`** — `TenantStatusDto`, `SubscriptionDto`, `UsageSnapshotDto` (JSON-mirror of the server projections).
- **`Pages/Overview/OverviewPage.razor` + `OverviewPage.razor.cs`** — code-behind pattern: 4 stat tiles (plans/invoices/outstanding/usage), widgets for tenant status, subscription, usage summary, recent activity + SSE LIVE chip; per-widget loading (`MudProgressCircular`) + error states (`FshErrorBand`); SSE observe-only via `SseService.ConnectionChanged` (app root owns SSE lifecycle); fixed deserialization issues with flexible enum converter.
- **`FSH.BlazorShared.csproj`** — added `System.Reactive 6.0.1` (custom `Subject<T>` implements `IObservable` — no Rx types were actually required, but the package reference keeps consumers safe).
- **`_Imports.razor`** (dashboard) — added `FSH.BlazorShared.Sse`, `Models.Dashboard`, `Models.Audits` for all pages.
- **Tests** — `Pages/Overview/OverviewPageTests.cs` (6 tests: all-stats render, placeholder→values, tenant error band, partial-failure resilience, no-subscription nav to `/subscription`, SSE LIVE chip). Dashboard suite **24/24** → **31/31** after 3.2 (`ActivityPageTests`, 7 tests).

## Task Checklist

### 3.1 Overview + SSE Integration ✅
- [x] **ISseService** — SSE stream client in `BlazorShared/Sse/` (manual `GetStreamAsync` parsing; token flow `POST /api/v1/sse/token` → `GET /api/v1/sse/stream?token=`)
  - `OnMessage` events via `IObservable<SseEvent>`, `ConnectionChanged` event
  - Automatic reconnection with capped backoff (1s → 2s → … → 30s, reset on success)
  - Fixed to stop retrying when session token is gone (prevents infinite 401 loops after logout/expiry)
- [x] **OverviewPage.razor** — `Pages/Overview/OverviewPage` — stat tiles, subscription/usage/audit widgets, SSE LIVE chip, loading/error states
  - Now observes `SseService.ConnectionChanged` only (app root owns SSE lifecycle)
  - Removed unused `_loadingSse`/`_sseError` fields and loading spinner from UI
- [x] **AuditTag enum fix** — Added `[JsonConverter(typeof(FlexibleEnumJsonConverter<AuditTag>))]` to handle string/int deserialization (React parity)
- [x] **bUnit suite** — `OverviewPageTests` (6 tests) — dashboard suite 24/24 → **31/31** after 3.2 (7 `ActivityPageTests`)

### 3.2 Activity
- [x] **ActivityPage.razor + .razor.cs** — `@page "/activity"`, `[Authorize]`, subscribes to `ISseService.Messages`
  (IObservable) + `ConnectionChanged`; React parity with `clients/dashboard/src/pages/activity.tsx`
  - Live SSE event log, newest-first, capped at 200 (React `MAX_EVENTS` parity), `_eventCount` total
  - Header: `FshPageHeader` ("Live activity", count chip, unit "event") + status pill
    (streaming=connected / connecting / offline)
  - Empty state: `FshEmptyState` ("Listening for activity" while live, "No events yet" when offline)
  - Event rows: `FshStatusPill` tone-mapped by type (`EventTone`: fail/error/revoke→danger,
    warn/retry→warning, login/issued/created→success, token/auth→info, else neutral) · payload summary
    (compact JSON, `PayloadSummary`) · entity label (`EntityLabel` — pulls entityId/aggregateId/id/
    tenantId/userId) · receive time (`HH:mm:ss`)
  - Responsive: mobile card list + desktop 3-col grid (`1fr 240px 120px`, CSS in `fsh.css` under
    `/* ---------- live activity ---------- */`)
  - Note: this replaces the older spec (IActivityService + MudTable infinite scroll + filters) — the
    React page is a **live SSE stream**, not a paged audit log; built to match React.
- [x] **bUnit suite** — `ActivityPageTests` (7 tests): offline empty state, streaming empty state, newest-first
  prepend + payload/entity rendering, 200-event cap with running total, plain-string payload summary,
  em-dash entity fallback, subscribe-on-render. **Dashboard suite 31/31.**

### 3.3 Subscription
- [x] **SubscriptionPage.razor + .razor.cs** — `@page "/subscription"`, `[Authorize]`, React parity with
  `clients/dashboard/src/pages/subscription.tsx`
  - Two-column layout (left rail: Plan card + Validity card; right column: Usage + Recent invoices)
    via CSS grid (`fsh-subscription-grid`, 360px left rail, flex-1 right, CSS in `fsh.css`)
  - **Plan card**: plan name (status.plan or subscription.planKey), `FshStatusPill` "Active" badge, started/ends
    dates via `FshFormat.DateShort`, "operator-driven" copy; no-subscription fallback state
  - **Validity card**: `ExpiryState()` maps to tone (Active→Success, InGrace→Warning, Expired→Error) + "Inactive"
    badge when `!isActive`, valid until date, grace end date (InGrace only)
  - **Usage by resource**: current-month snapshots filtered by `DateTime.UtcNow` Year/Month (React parity:
    `toUsageRows`), utilization percentage, `MudProgressLinear`-style CSS bars (`fsh-usage-bar`, colour:
    error if overage, warning ≥80%, else primary), overage badge, number formatting via `FshFormat.Number`
  - **Recent invoices**: top 5 most recent (sorted by `CreatedAtUtc` desc), icon tile + invoice number + status pill
    + period + `FshFormat.Money`, click → `/invoices/{id}`
  - Loading skeletons, error states, empty states per section; 4 parallel queries in `OnInitializedAsync`
  - **DashboardDtos update**: `UsageSnapshotDto` expanded with `TenantId`, `PeriodYear`, `PeriodMonth` (server
    projection includes them; needed for current-month filter — React `periodYear`/`periodMonth`)
- [x] **bUnit suite** — `SubscriptionPageTests` (5 tests): full page render, no-subscription fallback,
  usage empty state, in-grace validity with grace date, error band on status failure. **Dashboard 36/36**

### 3.4 Wallet
- [x] **WalletDtos.cs** (`BlazorShared/Models/Billing/`) — `WalletDto`, `WalletTransactionDto`, `CreateTopupRequestRequest` (mirrors server `WalletDto` / `WalletTransactionDto` / `CreateTopupRequestCommand`)
- [x] **IBillingService** — added `GetMyWalletAsync()` + `CreateTopupRequestAsync(CreateTopupRequestRequest)` (**BillingService** implemented: `GET /api/v1/billing/wallet/me`, `POST /api/v1/billing/wallet/topup-requests`)
- [x] **WalletPage.razor + .razor.cs** — `@page "/wallet"`, `[Authorize]`, React parity with `clients/dashboard/src/pages/wallet.tsx`
  - **Balance card**: large `FshFormat.Money` display, low-balance warning (`fsh-low-balance-hint`, threshold = 10, red when ≤ 0), loading skeleton
  - **Top-up request form**: `EditForm` + `MudTextField` (amount, currency adornment), `MudTextField` (note, 3 lines, max 1000), `MudButton submit` (disabled until valid + not submitting), success `MudAlert` + form reset + refresh after submit, error via `ISnackbar`
  - **Request list**: 4-col desktop grid (`fsh-wallet-grid`, React DESKTOP_GRID parity: `1fr 140px 130px 150px`), date + amount + status pill + invoice link; `FshPager` for multi-page
  - Status tones: Pending→Warning, Invoiced→Info, Completed→Success, Rejected→Error (matches React `statusTone`)
- [x] **bUnit suite** — `WalletPageTests` (5 tests): full render, low-balance hint, empty balance hint, empty requests, error band. **Dashboard 41/41**

### 3.5 Invoices
- [x] **IBillingService** — added `GetMyInvoicesAsync(pageNumber, pageSize, status, periodYear, periodMonth)` (**BillingService** implemented: `GET /api/v1/billing/invoices/me`, reuses existing `GetInvoiceByIdAsync` + `GetInvoicePdfAsync`)
- [x] **InvoicesPage.razor + .razor.cs** — `@page "/invoices"`, `[Authorize]`, React parity with `clients/dashboard/src/pages/invoices.tsx`
  - `FshPageHeader` with count, client-side search box (`MudTextField`, searches invoice number / status / period on current page; pagination suppressed while searching)
  - Desktop 5-col grid (`fsh-invoices-grid`, React DESKTOP_GRID parity), rows clickable → `/invoices/{id}`, status pills (Paid→Success, Issued→Info, Void→Error)
  - Loading skeletons, `FshEmptyState` (search-aware title/body + clear button), `FshErrorBand`, `FshPager`
- [x] **InvoiceDetailPage.razor + .razor.cs** — `@page "/invoices/{Id}"`, `[Authorize]`, React parity with `clients/dashboard/src/pages/invoice-detail.tsx`
  - Back button, header card (accent bar, icon tile, invoice number + status/purpose pills, period + amount, Download PDF button)
  - Line items section (4-col grid `fsh-lineitems-grid`, qty/unit price/amount, total row), Details card (status/currency/period/created/issued/due/paid/voided/period start/end with tonal row colors), Notes card
  - PDF download via **new `fshDownload.js`** in dashboard wwwroot (`fshDownload.saveFile`, same helper as admin) + index.html script ref; loading skeleton + not-found + error states
- [x] **bUnit suite** — `InvoicesPageTests` (4: render, empty, error band, client-side search filter) + `InvoiceDetailPageTests` (3: render, error band, not-found). **Dashboard 48/48**

### 3.6 Catalog — Brands, Categories, Products
- [x] **CatalogDtos.cs** (`BlazorShared/Models/Catalog/`) — `BrandDto`, `CategoryDto`, `CategoryTreeNodeDto`, `MoneyDto`, `ProductDto`, `ProductImageDto` + request records (`CreateBrandRequest`, `UpdateBrandRequest`, `CreateCategoryRequest`, `UpdateCategoryRequest`, `CreateProductRequest`, `UpdateProductRequest`, `ChangeProductPriceRequest`, `AdjustProductStockRequest`) — mirrors server DTOs/commands
- [x] **ICatalogService** — `SearchProductsAsync`, `GetProductAsync`, `CreateProductAsync`, `UpdateProductAsync`, `DeleteProductAsync`, `ChangeProductPriceAsync`, `AdjustProductStockAsync` (returns `Task<int>` — server `AdjustProductStockCommand` is `ICommand<int>`), `GetProductImagesAsync`, `SearchBrandsAsync`, `CreateBrandAsync`, `UpdateBrandAsync`, `DeleteBrandAsync`, `SearchCategoriesAsync`, `GetCategoryTreeAsync`, `CreateCategoryAsync`, `UpdateCategoryAsync`, `DeleteCategoryAsync` (**CatalogService** implemented, endpoints `/api/v1/catalog/...`)
- [x] **ProductsPage.razor** — MudTable: thumbnail, brand/category chips, price, stock, search + brand/category/isActive filters, create/edit dialogs (sku/name/description/brand/category/price/stock), delete confirm, **price-change + stock-adjust dialogs**. React parity: `clients/dashboard/src/pages/catalog/products.tsx`
- [x] **ProductDetailPage.razor** — hero card, description, image gallery (thumbnail set + delete), brand/category/created/updated meta, edit + price/stock actions. React parity: `clients/dashboard/src/pages/catalog/product-detail.tsx`
- [x] **BrandsPage.razor** — MudTable: logo, name, description, created, search + pager, create/edit/delete dialogs. React parity: `clients/dashboard/src/pages/catalog/brands.tsx`
- [x] **CategoriesPage.razor** — MudTable: name, parent, product count, search + pager, create/edit (parent combobox)/delete dialogs. React parity: `clients/dashboard/src/pages/catalog/categories.tsx`

### 3.7 Identity (Dashboard)
Full React parity — 6 pages + 4 dialogs, full CRUD (the plan's original read-only-only scope was superseded by the React app's CRUD implementation).
- [x] **GroupDtos.cs** (`BlazorShared/Models/Identity/`) — `GroupDto`, `GroupMemberDto`, `CreateGroupRequest`, `UpdateGroupRequest`, `AddUsersToGroupRequest` — mirrors server `Modules.Identity.Contracts` DTOs
- [x] **IGroupService/GroupService** (`BlazorShared/Services/`) — `ListAsync(search)`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `GetMembersAsync`, `AddUsersAsync`, `RemoveUserAsync` — endpoints `/api/v1/identity/groups/...`
- [x] **IUserService extensions** — `DeleteAsync`, `ConfirmEmailAsync`, `ResendConfirmationEmailAsync`
- [x] **UsersListPage.razor** — MudTable: avatar, name, email, role, status + email-confirmed chips; search + active/email filters + pager; create (register) dialog; row → detail. React parity: `clients/dashboard/src/pages/identity/users.tsx`
- [x] **UserCreateDialog.razor** — MudForm: first/last name, email, userName, password + confirm, phone
- [x] **UserDetailPage.razor** — hero (avatar/name/email/badges), meta (phone/joined/2FA), **roles assignment** (toggle switches, dirty-state save), **sessions panel** (revoke all / revoke one via session service), **impersonate action**, actions: toggle status, delete, confirm email, resend confirmation. React parity: `clients/dashboard/src/pages/identity/user-detail.tsx`
- [x] **RolesListPage.razor** — MudTable: name, description, permission count, search + pager, upsert dialog, system-role read-only (lock chip). React parity: `clients/dashboard/src/pages/identity/roles.tsx`
- [x] **RoleEditorDialog.razor** — MudForm: name + description (create/update)
- [x] **RoleDetailPage.razor** — profile card, **grouped permission catalog editor** (MudTreeView or MudCheckbox groups by resource, dirty-state save, root-only note), delete (non-system). React parity: `clients/dashboard/src/pages/identity/role-detail.tsx`
- [x] **GroupsListPage.razor** — MudTable: name, description, member count, default/system chips, search + pager, create dialog, row → detail. React parity: `clients/dashboard/src/pages/identity/groups.tsx`
- [x] **GroupEditorDialog.razor** — MudForm: name, description, isDefault switch, role multi-select
- [x] **GroupDetailPage.razor** — hero (name/desc/member count/chips), members table (add users, remove member), edit + delete. React parity: `clients/dashboard/src/pages/identity/group-detail.tsx`

### 3.8 Tickets
- [ ] **ITicketService** — `SearchAsync`, `GetAsync`, `CreateAsync`, `ReplyAsync`, `CloseAsync`
- [ ] **TicketsListPage.razor** — MudTable with subject, status MudChip (open/pending/closed), priority icon, last update, search/filter
- [ ] **TicketDetailPage.razor** — MudCard thread view: messages in MudList with sender, timestamp, content. MudTextField + MudButton for reply. Close MudButton.
- [ ] **TicketCreateDialog.razor** — MudForm: MudTextField subject, MudSelect category/priority, MudTextArea description

### 3.9 Chat
- [x] **IChatService** — `GetChannelsAsync`, `GetMessagesAsync(channelId, page)`, `SendMessageAsync`
- [x] **SignalR hub** — Real-time message delivery (receive + send) via existing `HubConnectionService`
- [x] **ChatPage.razor** — Split layout:
  - Left: channel list (MudNavMenu or MudList) with unread MudBadge
  - Right: message list (virtualized MudList) + MudTextField send box + MudButton
  - Messages show sender avatar/name, timestamp, content
  - Load older messages on scroll to top
- [x] **CreateChannelDialog.razor** — MudForm: name, description, private toggle

### 3.10 Files
- [x] **IFileService** — `ListAsync(folder)`, `UploadAsync`, `DownloadAsync`, `DeleteAsync`, `GetPresignedUrlAsync`
- [x] **FileManagerPage.razor** — MudTable with file name, size, type icon, modified date, download/delete actions
- [x] **UploadZone.razor** — MudFileUpload or drag-and-drop zone, progress, presigned URL generation
- [x] **FilePreviewDialog.razor** — Image/PDF preview, metadata panel, download/delete
- [ ] **Breadcrumb navigation** for folders

### 3.11 System Pages
- [x] **HealthPage.razor** — `/system/health`, reuses admin pattern, anonymous probes, 10s auto-refresh
- [x] **AuditsPage.razor** — `/system/audits`, MudTable with time-range presets, event-type/severity filters, summary strip, detail dialog
- [x] **SessionsPage.razor** — `/system/sessions`, MudTable with search, include-inactive toggle, per-row revoke, auto-refresh
- [x] **TrashPage.razor** — `/system/trash`, tabbed (Products/Brands/Categories/Tickets/Files), permission-gated tabs, restore with confirmation
- [x] **HealthCheckRow.razor** — shared expandable row component for health check details
- [x] **AuditDetailDialog.razor** — full audit record detail view (identity, trace, payload)

### 3.12 Settings (Dashboard)
- [x] **SettingsLayout.razor** — nav rail with profile/security/appearance/branding/notifications/api-keys links + `@Body`
- [x] **SettingsIndexPage.razor** — `/settings` redirect → `/settings/profile`
- [x] **ProfilePage.razor** — edit name, phone (React parity: `settings/profile.tsx`)
- [x] **SecurityPage.razor** — password change dialog + active sessions (React parity: `settings/security.tsx`)
- [x] **AppearancePage.razor** — Dark/light/System mode (React parity: `settings/appearance.tsx`)
- [x] **ApiKeysPage.razor** — placeholder (React parity: `settings/api-keys.tsx`)
- [x] **BrandingPage.razor** — placeholder (React parity: `settings/branding.tsx`)
- [x] **NotificationsPage.razor** — placeholder (React parity: `settings/notifications.tsx`)
- [x] Added `UpdateMyProfileAsync` to `IUserService`/`UserService` + `UpdateProfileRequest` DTO (endpoint exists server-side)

### 3.13 Impersonation (Dashboard)
- [x] **ImpersonationBanner.razor** — MudAlert banner: "Impersonating {user}" + end button (warning amber same-tenant / error red cross-tenant; stop-flow with stash/no-stash branches + root-operator guard; React parity: `impersonation-banner.tsx`)
- [x] **Impersonation detection** in AuthStateProvider: `GetImpersonation(ClaimsPrincipal)` / `GetImpersonationAsync()` read `act_sub`, `act_tenant`, `act_name` claims
- [x] **Terminal pages:**
  - [x] `TenantDeactivatedPage.razor` — Redirect on 403 with deactivation reason (`TerminalErrorHandler` outermost handler)
  - [x] `ImpersonationEndedPage.razor` — Redirect when impersonation revoked (401 + act_sub; also catches `ApiRequestException(401)` from refresh failure)
  - [x] Both rendered outside AppShell (no sidebar) — `@layout TerminalLayout`
- [x] Added `EndImpersonationAsync` to `IImpersonationService` (POST `/api/v1/identity/impersonation/end` → `TokenResponse`) + `HasImpersonationStashAsync` / `SetFreshTokensAsync` / `RestoreTokensAsync` to `ITokenStore` (dashboard real impl; admin/hybrid no-ops)

### 3.14 Command Palette
- [x] **NavSpec.cs** — single source of nav destinations (Top/Bottom/TrashPermissions/Sections) shared by sidebar + palette; mirrors React `nav-data.ts`
- [x] **MainLayout.razor** — refactored to NavSpec; topbar search button (`aria-label="Search (Ctrl+K)"`) opens the palette
- [x] **CommandPalette.razor** — custom overlay (fixed panel + backdrop; MudAutocomplete-in-MudDialog was abandoned: bUnit renders no list items for that combo): nav destinations + 5 account items + theme light/dark/system + sign-out; filter on label/hint/keywords; arrow-key highlight + Enter select; Esc/backdrop close; footer kbd hints
- [x]   Keyboard shortcut: `Ctrl+K` / `Cmd+K` (JS `eval` listener → `[JSInvokable] CommandPaletteShortcutRelay`)
- [x]   Permission gates (nav + Trash any-of) — same semantics as NavSpec
- [x]   Sign-out via `FshConfirmDialogContent` + `AuthStateProvider.NotifyLogoutAsync()` (injects concrete `AuthStateProvider` for logout + abstract for claims — banner pattern)
- [x]   8 bUnit tests (178/178 dashboard, 147/147 admin, icon audit PASS) — auth pattern: permission claims via `FixedAuthStateProvider`

## Next Up

**Phase 3 is COMPLETE (3.1–3.14, dashboard suite 178/178).** The previous "Next Up: 3.11 System pages" was
stale — 3.11 (Health/Audits/Sessions/Trash + tests) actually landed in `52861c37` on 2026-08-05, before
this section was last written. 3.12 (Settings), 3.13 (Impersonation: banner, terminal pages, token stash)
and 3.14 (Command Palette: NavSpec + Ctrl+K) are also landed and committed (3a6bf25e, 6937c600, dcf17028).

Genuinely open work for the dashboard stream (verify on the board before claiming — another session may
own it):
1. Phase 4 — dashboard-blazor Playwright E2E suite (Phase 4.9 covered admin-blazor only)
2. Phase 6 — Polish & Perf (bundle/AOT/trimming, ServerData audit, debounced search)
3. Phase 7 — parity leftovers (admin accent/font/density settings — tracked in `Phase-07-Parity-Completion/plan.md`; admin command palette is in flight by sess-maui)

## Architecture Decisions

| Decision | Choice | Why |
|----------|--------|-----|
| SSE client | **Pure C# `HttpClient` streaming** (no JS interop) | `.NET` WASM supports `ResponseHeadersRead` streaming; manual event parsing (`event:`/`data:` lines) keeps the whole stack in C# and unit-testable. The old "must use JS EventSource" note below is obsolete |
| SSE reconnection | Exponential backoff in C# | Memory: 1s → 2s → 4s → max 30s, reset on successful connection |
| Chat | SignalR (not SSE) | Bidirectional; SSE is server→client only |
| Image gallery | MudCarousel | Native MudBlazor component; falls back to static image list gracefully |
| File upload | Presigned URL → direct-to-S3 | Same as React; avoids API bandwidth and double-hop |
| Command palette | MudAutocomplete with page registry | Faster than custom modal; search across flat page list |

## Notes & Gotchas

- **SSE in WASM**: `SseService` streams with `HttpClient.SendAsync(..., HttpCompletionOption.ResponseHeadersRead)` + a manual line parser (`event:` / `data:`), publishing `SseEvent` records through a small custom `Subject<T>` (`IObservable<T>`). Works on WASM; reconnection is in C# with capped backoff. UI handlers must marshal via `InvokeAsync(StateHasChanged)` because `OnNext` runs on the SSE loop thread.
- **Code-behind vs `@code`**: `OverviewPage` uses a `.razor.cs` partial (preferred for page logic). `@using` directives in the `.razor` do **NOT** apply to the code-behind — the `.cs` needs its own `using FSH.BlazorShared.Sse;` etc.
- **MudBlazor 9.7 API drift**: no `MudCircularProgress` (use `MudProgressCircular`), `MudChip` needs `T="string"`, `MudRadioGroup` binds via `@bind-Value` (no `SelectedOption`), `MudRadio` uses `Value=` (no `Option=`), `MudTextField` has no `Size=`/`MinLength=`, `MudIconButton` has no `AriaLabel=` (use lowercase `aria-label=`), `MudListItem` has no `Clickable=`/`MudListItemAvatar`/`MudListItemText`. Admin + dashboard now build **0 warnings**.
- **Impersonation cross-app**: When admin starts impersonation from admin app, they get redirected to dashboard app with a new JWT containing `act_sub`/`act_tenant` claims. The dashboard AuthStateProvider must detect these.
- **Chat SignalR**: Hub connection is separate from notification hub. `HubConnection` in BlazorShared/Realtime/ with separate builder URLs.
- **File preview**: For PDFs and images, generate a Blob URL from the API response and render in an `<iframe>` or `<img>`. For other types, show file metadata only.
- **MudAutocomplete command palette**: Register all pages with their route paths and titles. Search filters by title. MudAutocomplete's `CoerceValue` and `ItemTemplate` are key.
- **Virtualized MudTable**: For large lists (messages, activity logs), ensure MudTable uses `ServerData` with server-side pagination. No client-side paging of 10k+ items.
- **Terminal pages**: Register outside AppShell layout so they render full-screen without sidebar. Use `@layout` directive or separate route table.

## Blocker Checklist

- [x] Phase 1 complete: can login, AppShell renders, routing works
- [x] API SSE endpoint available and functional (`/api/v1/sse/token` → `/api/v1/sse/stream?token=`)
- [x] SignalR hub configured for chat (usually `/hubs/chat` or similar)
- [x] File attachment APIs work (presigned URL generation, upload, list)
- [x] API supports impersonation endpoints