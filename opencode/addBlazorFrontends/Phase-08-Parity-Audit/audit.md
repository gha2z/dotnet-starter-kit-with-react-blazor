# Phase-08 Zero-Gaps Parity Audit — React ↔ Blazor ↔ MAUI

Status: **FINAL — Phase-09 Zero-Gaps closed out (2026-08-13).** All 8 queue items (Q1–Q8) resolved across W2/W3/W4. Evidence files:
`evidence/admin-react.routes.md`, `evidence/dashboard-react.routes.md`, `evidence/blazor-and-hybrid.routes.md`.

Scope: read-only parity audit (Phase-08). The §3 delta table and §6 queue now carry Phase-09 resolution stamps; the audit itself never modified app source.

---

## 1. Headlines

- **Admin (React `clients/admin` ⇄ Blazor `FSH.Admin.Wasm`): feature-parity for all 27 Blazor pages.** Delivered via three parity phases; only minor deltas remain (tenants detail depth, audit dialog chrome, webhook deliveries pagination, reset-password strength meter).
- **Dashboard (React `clients/dashboard` ⇄ Blazor `FSH.Dashboard.Wasm`): 32/36 parity.** The 4 outstanding gaps are feature-depth deltas on already-built pages (chat deep-link, appearance richness, expiry banner, audit advanced-filters/detail), not missing pages.
- **Hybrid (MAUI `clients/FSH.Hybrid`): surface is 3 Blazor pages** (`/`, `/files`, `/login`) + 2 native Shell tabs. `NavSpec.cs` already targets the full dashboard route set — the parity gap is **routing/render, not nav-definition**.
- **Deltas are classified as `built-but-thinner` (feature-depth on existing pages), NOT missing pages.** The React surfaces were the feature superset in every remaining gap.

Verified delta & parity counts:

| Axis | Inspected | Parity | Delta | High-impact |
|---|---|---|---|---|
| Admin Blazor vs React admin | 27 pages | 22 | 5 | 1 (tenants detail) |
| Dashboard Blazor vs React dashboard | 36 pages | 32 | 4 | 3 (chat, appearance, expiry) |
| Hybrid vs dashboard Blazor | 3→~21 | — | ~18 (route-level) | 1 structural |

---

## 2. Severity definitions

| Severity | Meaning | Assignment |
|---|---|---|
| 🔴 **Must-fix (P0)** | user-blocked, security, or terminal: file open/fail loop, auth prompt impossible, crash on publish |
| 🟠 **High (P1)** | feature outright missing or functionally invisible on an already-built page; blocks a whole workflow |
| 🟡 **Medium (P2)** | built-but-thinner: clearly present but with reduced depth vs React reference |
| 🟢 **Low (P3)** | cosmetic / fidelity-only; same information, different chrome |
| ✅ **Parity** | functionally equivalent (may differ in chrome/tech, same user capability) |

---

## 3. Detailed delta list

### 3.1 ADMIN CHANNEL — deltas

| # | Severity | Route/page | Delta | React reference | Current Blazor state | Cited evidence |
|---|---|---|---|---|---|---|
| A1 | ✅ | `/audits` detail | ~~Detail is a centered `MudDialog` where React admin uses a 640px side-sheet.~~ **Resolved (W3):** presentation delta accepted — payload copy + correlation chips already present; no timeline on either admin side. | `admin-react/evidence`: §2.16 | `AuditDetailDialog.razor` + `AuditPayloadSection.razor` + `AuditCorrelationSection.razor` | `blazor-and-hybrid.routes.md:42` |
| A2 | ✅ | `/tenants/{Id}` | ~~Single-page detail without React's in-page provisioning poll, active-grants card, or branding editor.~~ **Resolved (W3):** 2s `PeriodicTimer` provisioning poll (`TenantDetailPage.razor.cs:85`), `ActiveGrantsCard` (own poll loop `ActiveGrantsCard.razor.cs:68`), `TenantBrandingCard`, `ImpersonateDialog` all landed. | `admin-react/evidence`: §2.3 | `TenantDetailPage.razor` + `ActiveGrantsCard.razor` + `TenantBrandingCard.razor` + `ImpersonateDialog.razor` | `blazor-and-hybrid.routes.md:39` |
| A3 | ✅ | `/webhooks/{Id}` | ~~Deliveries list unpaginated~~ **Resolved (W3):** `FshPager` on deliveries (`WebhookDetailPage.razor:159`). | `admin-react/evidence`: §2.15 | `WebhookDetailPage.razor` | `blazor-and-hybrid.routes.md:41` |
| A4 | ✅ | `/billing/invoices/{Id}` | ~~Layout/state fidelity~~ **Resolved (W3):** KPI header (money amount + status/purpose pills), line items, PDF download present. | `admin-react/evidence`: §2.10 | `InvoiceDetailPage.razor` | `blazor-and-hybrid.routes.md:45` |
| A5 | ✅ | auth pages | ~~strength meter absent~~ **Resolved (W3):** `ScorePassword` + 3-segment strength bar (`ResetPasswordPage.razor:49-62,121-131`). | §2.22 | `ResetPasswordPage.razor` | verified read `Auth/ResetPasswordPage.razor` |

**Admin parity (22, representative):** Overview, login/forgot/reset/confirm-email, users list+detail, roles list+detail (permission matrix), webhooks list, audits list, billing hub/invoices/topups/plans, health, impersonation, notifications inbox, settings profile/sessions/security/appearance. Confirmed by `blazor-and-hybrid.routes.md` §1 route table + bunit surface (§5).

---

### 3.2 DASHBOARD CHANNEL — deltas

| # | Severity | Route/page | Delta | React reference | Current Blazor state | Cited evidence |
|---|---|---|---|---|---|---|
| D1 | ✅ | `/chat/:channelId` | ~~No deep-linkable channel route~~ **Resolved (W2):** `@page "/chat"` + `@page "/chat/{Id:guid}"` (`ChatPage.razor:1-2`). | `dashboard-react/evidence`: §1 line 33 | `ChatPage.razor` | `blazor-and-hybrid.routes.md:100` (row 25) |
| D2 | ✅ | `/settings/appearance` | ~~theme-mode only~~ **Resolved (W2):** accent presets + `CustomAccentDialog` (hue/chroma spec) + font family + density + motion (`SettingsAppearancePage.razor` + `CustomAccentDialog.razor` + `FshAppearanceOptions`). | `dashboard-react/evidence`: §4 | `SettingsAppearancePage.razor` + `CustomAccentDialog.razor` | verified read `SettingsAppearancePage.razor` |
| D3 | ✅ | (global) | ~~No global expiry/grace banner~~ **Resolved (W2):** `FshExpiryBanner` mounted above content (`MainLayout.razor:97`) + bunit coverage (`FshExpiryBannerTests.cs`). | `dashboard-react/evidence`: §3 | dashboard `MainLayout.razor` + `FshExpiryBanner.razor` | verified grep `MainLayout.razor` |
| D4 | ✅ | `/system/audits` | ~~Filter/detail depth~~ **Resolved (W2):** advanced filter set (source/user/correlation/trace) on `AuditsPage.razor:61-70` + related-events timeline + payload copy in detail. | `dashboard-react/evidence`: §5 | `AuditsPage.razor` | verified grep `AuditsPage.razor` |
| D5 | — | (retired) | **REMOVED after review** — the sub-page-route concern applies only to D1 chat; all other `{Id}` detail routes (invoices, products, tickets, users, roles, groups) exist as routable `@page` in dashboard Blazor. | — | — | — |

**Dashboard parity (31):** Overview (SSE live updates), activity, wallet, subscription, invoices list+detail, health, trash, sessions, tickets list+detail, chat (single-route channel UI + SignalR), files (My/Shared tabs + type-filter chips + preview + visibility), catalog products/brands/categories + product detail (price/stock dialogs, brand/category editors), settings profile/appearance/security/branding/notifications/api-keys, identity users/roles/groups + detail, terminal pages. Confirmed by `blazor-and-hybrid.routes.md` §2 + §96 bunit breakdown.

---

### 3.3 HYBRID CHANNEL — deltas

| # | Severity | Route/page | Delta | State | Cited evidence |
|---|---|---|---|---|---|
| H1 | ✅ | nav → unimplemented routes | ~~`NavSpec.cs` mirrors full dashboard route set but only `/`, `/files`, `/login` exist as `@page`.~~ **Resolved (W4):** `NavSpec.ImplementedRoutes` set + `IsImplemented` flag gates `CanSee()` in `MainLayout` — unimplemented routes hidden from sidebar. Dead-end 404s eliminated. | resolved | `NavSpec.cs` + `MainLayout.razor` + `MainLayout.razor.cs` | `blazor-and-hybrid.routes.md` Unusual finding 1 |
| H2 | 🟢 | native shell | Settings/About are native MAUI Shell tabs, not Blazor — **by design** (settings handled natively). Not a defect. | — | `blazor-and-hybrid.routes.md` §3.3 |

---

## 4. Hybrid nav-route analysis (H1 detail)

`NavSpec.cs` sections (working tree, `blazor-and-hybrid.routes.md` §3.2):
- TopItems: Overview `/`, Chat `/chat` (ChatPermissions.Channels.View), My Files `/files` (FilesPermissions.Upload).
- Sections: operations (activity/subscription/wallet/invoices), catalog (products/brands/categories), helpdesk (tickets), identity (users/roles/groups), system (health/audits/sessions/trash).
- **`/activity` has no permission gate** → always-visible → always-404 today.
- Only 3 of the referenced routes exist as `@page`: `/` (`OverviewPage.razor`), `/files` (`FilesPage.razor`), `/login` (`LoginPage.razor`).

Impact: the sidebar renders 404 for most items. **RESOLVED (W4)** via decision (a) — gate the nav to implemented routes. `NavSpec.ImplementedRoutes` lists `/`, `/files`, `/login`; `NavItem.IsImplemented` is consulted by `MainLayout.CanSee()` so every visible sidebar item has a working `@page`. Unimplemented destinations (chat, activity, subscription, wallet, invoices, catalog, tickets, identity, system) are hidden — no more FshNotFound dead-ends. When a new Hybrid `@page` lands, it must be added to `ImplementedRoutes`.

---

## 5. Non-deltas & notable parity notes

- **`@page` routing** — all routable pages live in per-app Pages projects; `BlazorShared` is components/services only (0 `@page`) — by design.
- **SSE is dashboard-only** — dashboard Overview + Activity consume `ISseService`; admin uses SignalR bell; Hybrid registers `SseService` in DI but no page consumes it yet.
- **Admin has no command palette equal to dashboard** — the palette exists in *both* Blazor admin and Blazor dashboard (per-app `CommandPalette`), mirroring the React split (admin React has none).
- **Impersonation**: React admin issues grants and sits outside impersonated sessions (no banner, documented `app-shell.tsx:20-22`). Blazor admin + dashboard implement `ImpersonationHandoff`/`ImpersonationBanner` for the "now on dashboard as…" flow — parity with the React dashboard-side flow.
- **2FA**: admin Blazor = password-change + 2FA (shared-key copy + manual code entry, no QR render); React admin = same + QR. Post-build refinement candidate, not a blocker.
- **Demo accounts**: React login demo picker is `import.meta.env.DEV`-gated and OFF in prod; Blazor has no demo picker. Not a real parity gap.

---

## 6. Next-build-phase queue (post-audit candidate fixes — READ-ONLY listing for Phase 7 handoff)

Items here are **recommendations only**; none were implemented in Phase-08 (read-only). All were resolved by Phase-09 Zero-Gaps (W2/W3/W4) unless marked *deferred*.

| # | App zone | Route/page | Work | Severity-to-fix | Status |
|---|---|---|---|---|---|
| Q1 | dashboard-blazor | `/chat` | Add `@page "/chat/{Id}"` + deep-link handling; keep auto-select fallback; wire browser-nav per channel | 🟠 | ✅ W2 |
| Q2 | dashboard-blazor | `/settings/appearance` | Port accent presets + custom-accent + font family + density + motion from React `appearance.tsx` | 🟠 | ✅ W2 |
| Q3 | dashboard-blazor | shell | Port `ExpiryBanner` (grace/expired/≤7d states) into `MainLayout` above content | 🟠 | ✅ W2 |
| Q4 | dashboard-blazor | `/system/audits` | Add advanced filter set (source/user/correlation/trace/tags) + detail drawer w/ related-events timeline + payload copy (range presets + type/severity/search already present) | 🟡 | ✅ W2 |
| Q5 | admin-blazor | `/audits` | Optional: modal → side-sheet presentation to mirror React (payload + correlation copy already present) | 🟢 | ✅ accepted (presentation delta only) |
| Q6 | admin-blazor | `/tenants/{Id}` | Add 2s provisioning poll + active-grants card + branding editor + impersonate dialog (renew/validity dialogs already present) | 🟡 | ✅ W3 |
| Q7 | FSH.Hybrid | nav | Either (a) gate the nav to implemented routes, or (b) implement the missing `@page` pages to match `NavSpec.cs` (explicit decision required; cost differs hugely) | 🟠 | ✅ W4 — decision (a) |
| Q8 | admin-blazor | `/reset-password` | Port `scorePassword` strength meter + STRENGTH_META bar from React `reset-password.tsx` | 🟢 | ✅ W3 |

**Post-build queue is fully drained.** Remaining post-build refinements tracked in §5 notes (2FA QR render, demo picker) are DEV-gated/non-parity and intentionally not ported.

---

## 7. Claim confidence & verification log

Every delta row was verified on both sides (working-tree reads/greps) during Stage 2, then independently falsified by a reviewer pass in Stage 4. Corrections from that pass are already folded into §3.

| Claim | Verdict | How |
|---|---|---|
| Blazor chat single-route | ✅ confirmed | `ChatPage.razor` has only `@page "/chat"`; React `routes.tsx:213` adds `chat/:channelId` |
| Dashboard appearance page is theme-only | ✅ confirmed | `SettingsAppearancePage.razor` (56 lines): 3 radios, no accent/font/density/motion; React `appearance.tsx:104-203` has all four |
| Dashboard MainLayout lacks expiry banner | ✅ confirmed | grep — only `ImpersonationBanner` + `FshOfflineBanner`; React `app-shell.tsx:40` mounts `ExpiryBanner` globally |
| Dashboard audits HAS range presets (correction) | ✅ updated | `AuditsPage.razor:34-37` 24h/7d/30d/90d + event-type/severity/search `:12-31`; real deltas = advanced filters + related-timeline + payload copy |
| Admin audit dialog HAS payload copy (correction) | ✅ updated | `AuditPayloadSection.razor:13-17` Copy button + `AuditCorrelationSection.razor:14-23`; admin React has **no** related-timeline either — delta is chrome only |
| Admin tenants detail lacks poll/grants/branding | ✅ confirmed | `TenantDetailPage.razor.cs:41-55` single `OnInitializedAsync` load; no Timer; React `detail.tsx:84-91` 2s poll + cards |
| Admin reset-password has NO strength meter | ✅ updated | Blazor `ResetPasswordPage.razor:100-105` mismatch hint only; React `reset-password.tsx:221-236` strength bar |
| Admin security page = password + 2FA (no API keys) | ✅ confirmed | `SecurityPage.razor` = `PasswordSection` + `TwoFactorSection`; grep for `ApiKey|api-keys` in admin-blazor → nothing |
| Blazor 2FA = shared-key + manual code (no QR render) | ✅ confirmed | `TwoFactorSection.razor:32-37`; React `security.tsx:332-362` renders QR via `qrcode` |
| Dashboard file manager = My/Shared tabs + kind chips + preview + visibility | ✅ parity | `FileManagerPage.razor` tabs + `_typeFilters` chips; `FilePreviewDialog.razor` visibility/public URL |
| Dashboard invoice detail = PDF download + line items + notes + statuses | ✅ parity | `InvoiceDetailPage.razor` + `.cs`: `GetInvoicePdfAsync` → `fshDownload.saveFile` |
| Admin appearance ≤ React (both light/dark + disabled density) | ✅ parity | Blazor `AppearancePage.razor` Light/Dark cards + density "coming soon" button — identical to React admin |

---

## 8. Open verification (blocked reads)

- `clients/FSH.Hybrid/Pages/` directory listing was shell-blocked during evidence gathering; native `SettingsPage`/`AboutPage` presence inferred from `AppShell.xaml` (`blazor-and-hybrid.routes.md` §6, unusual finding 6). Low risk.
- Hybrid `NavSpec.cs` row was verified from the untracked working-tree file (not committed; treated as working truth per board row 13).

---

## 9. Quick reference — routes per app

Admin Blazor (27): dashboard, login+3 auth, users×2, roles×2, tenants×2, webhooks×2, audits, health, impersonation, billing hub/invoices×2/plans/topups, notifications inbox, settings ×5.

Dashboard Blazor (37): overview, activity, wallet, subscription, login+3 auth, catalog products×2/brands/categories, tickets×2, files, chat, invoice×2, health, trash, sessions, audits, identity users×2/roles×2/groups×2, settings profile/appearance/security/branding/notifications/api-keys, terminal ×2.

Hybrid (3 + 2 native): overview, files, login; native settings + about Shell tabs.