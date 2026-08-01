# Phase 3 — Dashboard Feature Pages

> **Target:** All tenant-facing dashboard pages built — Overview (SSE), Activity, Subscription, Wallet, Catalog, Invoices, Identity (profile/user/role), Tickets, Chat, Files, System. Feature parity with `clients/dashboard` React app.

## Status

- Phase 3: **🟨 In progress** — 3.1 partial (SSE infra ✅ from Phase 1 + parity sprint; Overview ✅; ActivityFeed pending) — next: 3.2 Activity
- Prerequisites: Phase 1 ✅ (auth+login working, AppShell)
- **Parity sprint deliverables already in place:** `SseService` (token flow `POST /api/v1/sse/token` → `GET /api/v1/sse/stream?token=`, backoff reconnect, `ConnectionChanged` event), SSE status dot in topbar, full sidebar (accordion, permission-gated) — no rebuilds needed, only page work.
- Terminal pages `/tenant-deactivated` + `/impersonation-ended` do **not** exist yet (docs previously claimed they did) — tracked as 3.15 in `Phase-07-Parity-Completion/plan.md`.

## Task Checklist

### 3.1 Overview + SSE Integration 🟨
- [x] **ISseService** — SSE stream client in `BlazorShared/Sse/` (manual `GetStreamAsync` parsing; token flow `POST /api/v1/sse/token` → `GET /api/v1/sse/stream?token=`)
  - `OnMessage` events via `IObservable<SseEvent>`, `ConnectionChanged` event
  - Automatic reconnection with capped backoff (1s → 2s → … → 30s, reset on success)
- [x] **OverviewPage.razor** — `Pages/Overview/OverviewPage` — stat tiles, live-data ready
- [ ] **ActivityFeed.razor** — Virtualized list of recent activities
  - MudList with MudListItem for each activity
  - SSE updates prepend new items

### 3.2 Activity
- [ ] **IActivityService** — `SearchAsync(page, filters)`
- [ ] **ActivityLogPage.razor** — MudTable with infinite scroll
  - Filter by type, date range, user
  - Timestamp, action, resource, details

### 3.3 Subscription
- [ ] **ISubscriptionService** — `GetCurrentAsync`, `GetPlansAsync`, `UpgradeAsync`, `GetUsageAsync`
- [ ] **SubscriptionPage.razor** — MudCard with plan name, features list (MudList with check marks), usage progress bars (MudProgressLinear), expiry date
- [ ] **UpgradeDialog.razor** — MudDialog: plan comparison, MudSelect for target plan, MudNumericField for period

### 3.4 Wallet
- [ ] **IWalletService** — `GetBalanceAsync`, `TopUpAsync`, `GetTransactionsAsync`
- [ ] **WalletPage.razor** — MudCard: balance display (large text), top-up MudButton → MudDialog (MudNumericField + MudSelect for payment method)
- [ ] **TransactionListPage.razor** — MudTable with date, description, amount (+/- with color), status MudChip

### 3.5 Invoices
- [ ] **IInvoiceService** — `SearchAsync`, `GetDetailAsync`, `DownloadAsync`
- [ ] **InvoicesListPage.razor** — MudTable with invoice number, date, amount, status MudChip, download MudButton
- [ ] **InvoiceDetailPage.razor** — MudCard sections: from/to info, line items (MudTable), total, PDF preview (iframe or Blob URL)

### 3.6 Catalog — Brands, Categories, Products
- [ ] **ICatalogService** — `SearchProductsAsync`, `GetProductAsync`, `SearchBrandsAsync`, `SearchCategoriesAsync`
- [ ] **ProductsListPage.razor** — MudTable with thumbnail (MudImage or MudAvatar), brand/category MudChips, price formatting, search
- [ ] **ProductDetailPage.razor** — MudCard with image gallery (MudCarousel), specs table (MudTable), description
- [ ] **BrandsListPage.razor** — MudTable with logo, name, product count
- [ ] **CategoriesListPage.razor** — MudTable with name, icon, product count

### 3.7 Identity (Dashboard)
- [ ] **ProfilePage.razor** — MudForm: edit profile info, MudFileInput for avatar, MudButton for save
- [ ] **UsersListPage.razor** — MudTable: tenant-scoped user list (view only, no edit/delete)
- [ ] **RolesListPage.razor** — MudTable with permission viewer (read-only MudTreeView)

### 3.8 Tickets
- [ ] **ITicketService** — `SearchAsync`, `GetAsync`, `CreateAsync`, `ReplyAsync`, `CloseAsync`
- [ ] **TicketsListPage.razor** — MudTable with subject, status MudChip (open/pending/closed), priority icon, last update, search/filter
- [ ] **TicketDetailPage.razor** — MudCard thread view: messages in MudList with sender, timestamp, content. MudTextField + MudButton for reply. Close MudButton.
- [ ] **TicketCreateDialog.razor** — MudForm: MudTextField subject, MudSelect category/priority, MudTextArea description

### 3.9 Chat
- [ ] **IChatService** — `GetChannelsAsync`, `GetMessagesAsync(channelId, page)`, `SendMessageAsync`
- [ ] **SignalR hub** — Real-time message delivery (receive + send)
- [ ] **ChatPage.razor** — Split layout:
  - Left: channel list (MudNavMenu or MudList) with unread MudBadge
  - Right: message list (virtualized MudList) + MudTextField send box + MudButton
  - Messages show sender avatar/name, timestamp, content
  - Load older messages on scroll to top

### 3.10 Files
- [ ] **IFileService** — `ListAsync(folder)`, `UploadAsync`, `DownloadAsync`, `DeleteAsync`, `GetPresignedUrlAsync`
- [ ] **FileManagerPage.razor** — MudTable with file name, size, type icon, modified date, download/delete actions
- [ ] **UploadZone.razor** — MudFileUpload or drag-and-drop zone, progress, presigned URL generation
- [ ] **Breadcrumb navigation** for folders

### 3.11 System Pages
- [ ] **HealthPage.razor** — MudCard grid: API, DB, Redis, MinIO status, uptime
- [ ] **AuditLogPage.razor** — MudTable (tenant-scoped audit trail, view only)
- [ ] **TrashPage.razor** — MudTable with deleted items, restore MudButton
- [ ] **SessionsPage.razor** — MudTable with active sessions, revoke MudButton

### 3.12 Settings (Dashboard)
- [ ] **ProfileSettingsPage.razor** — Edit name, email, timezone, avatar
- [ ] **ThemeSettingsPage.razor** — Dark/light MudSwitch, accent color (rose/indigo/violet/sky/emerald/amber) via MudSelect or MudButtonGroup

### 3.13 Impersonation (Dashboard)
- [ ] **ImpersonationBanner.razor** — MudAlert banner: "Impersonating {user}" + end button
- [ ] **Impersonation detection** in AuthStateProvider: read `act_sub`, `act_tenant` claims
- [ ] **Terminal pages:**
  - `TenantDeactivatedPage.razor` — Redirect on 403 with deactivation reason
  - `ImpersonationEndedPage.razor` — Redirect when impersonation revoked
  - Both rendered outside AppShell (no sidebar)

### 3.14 Command Palette
- [ ] **CommandPalette.razor** — MudAutocomplete with quick-nav to all pages
  - Keyboard shortcut: `Ctrl+K` / `Cmd+K`
  - Search page titles, navigate on select

## Next Up

**Task 3.1**: ISseClient + Overview page.

1. Read React reference: `clients/dashboard/src/pages/overview.tsx`, `clients/dashboard/src/lib/sse.ts`
2. Create `ISseClient` + `SseClient` in BlazorShared/Sse/
3. Wire in dashboard `Program.cs` (`AddScoped`)
4. Create `OverviewPage.razor` with MudCard grid
5. Subscribe to SSE stream → update stats live
6. Test: run dashboard app, verify SSE connects and data flows

## Architecture Decisions

| Decision | Choice | Why |
|---|---|---|
| SSE client | JS interop wrapping `EventSource` | Blazor WASM has no built-in SSE client; JS interop matches React pattern |
| SSE reconnection | Exponential backoff in C# | Memory: 1s → 2s → 4s → max 30s, reset on successful connection |
| Chat | SignalR (not SSE) | Bidirectional; SSE is server→client only |
| Image gallery | MudCarousel | Native MudBlazor component; falls back to static image list gracefully |
| File upload | Presigned URL → direct-to-S3 | Same as React; avoids API bandwidth and double-hop |
| Command palette | MudAutocomplete with page registry | Faster than custom modal; search across flat page list |

## Notes & Gotchas

- **SSE in WASM**: Blazor's `HttpClient` cannot stream SSE (it buffers responses). Must use JS interop for native `EventSource`. The C# SSE client receives parsed messages from JS.
- **Impersonation cross-app**: When admin starts impersonation from admin app, they get redirected to dashboard app with a new JWT containing `act_sub`/`act_tenant` claims. The dashboard AuthStateProvider must detect these.
- **Chat SignalR**: Hub connection is separate from notification hub. `HubConnection` in BlazorShared/Realtime/ with separate builder URLs.
- **File preview**: For PDFs and images, generate a Blob URL from the API response and render in an `<iframe>` or `<img>`. For other types, show file metadata only.
- **MudAutocomplete command palette**: Register all pages with their route paths and titles. Search filters by title. MudAutocomplete's `CoerceValue` and `ItemTemplate` are key.
- **Virtualized MudTable**: For large lists (messages, activity logs), ensure MudTable uses `ServerData` with server-side pagination. No client-side paging of 10k+ items.
- **Terminal pages**: Register outside AppShell layout so they render full-screen without sidebar. Use `@layout` directive or separate route table.

## Blocker Checklist

- [ ] Phase 1 complete: can login, AppShell renders, routing works
- [ ] API SSE endpoint available and functional (/api/v1/dashboard/stream or similar)
- [ ] SignalR hub configured for chat (usually `/hubs/chat` or similar)
- [ ] File attachment APIs work (presigned URL generation, upload, list)
- [ ] API supports impersonation endpoints
