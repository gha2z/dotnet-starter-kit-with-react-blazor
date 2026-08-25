# Implementation Log — Phase 09: UI Parity Hardening

> Agent-authored while executing plan.md. One section per wave: what changed, why, evidence
> (probe results / screenshots / test counts), deviations from plan, lessons.

## Setup (2026-08-24)

- Workflow restructure landed first (START-HERE.md, PROJECT.md, STATUS.md dashboard, mission
  folders, protocol updates) — see commit history.
- Stack state at wave start: user rebuilt + relaunched Aspire AppHost; services on
  7030/5175/5176.

(Waves append below as they complete.)

## P1 — Chat UX (2026-08-24) — ✅

**What changed**
- `ChatPage.razor.cs`: render-aware scroll — `ScrollToBottom()` now sets `_scrollPending` and the
  actual scroll runs in `OnAfterRenderAsync` **after** Blazor's DOM patch (the old code scrolled
  synchronously and lost the race with the render queue, so new messages landed below the fold —
  the root cause of both "no auto-scroll on send" and "incoming not visible instantly").
- React pinning contract (message-list.tsx): incoming messages only auto-scroll when the feed is
  within 150px of the bottom (`IsNearBottomAsync` via new `distanceFromBottom()` in
  `fshChatScroll.js`); otherwise `_unseenCount++` and a jump-to-bottom pill appears
  ("↓ N new messages", absolute-positioned above the composer; conversation pane got
  `position: relative`). Own sends always pin.
- DM rail (spec §1c): partner avatar (photo → initials fallback) + green presence dot, replacing
  the generic person icon — `PartnerOf()` resolves the non-self member; reuses
  `GetAvatarUrl`/`InitialsFor`/`IsUserOnline`.

**Evidence**
- `walkthrough/probe-chat-scroll.mjs` — **5 PASS / 0 FAIL**: own send pins to bottom (0px);
  scrolled-up + incoming → no yank (545px preserved) + pill appears; pill click re-pins (0px);
  pinned + incoming → auto-scrolls (0px).
- `walkthrough/probe-chat-realtime.mjs` — **16 PASS / 0 FAIL** (regression, realtime intact).
- `walkthrough/evidence/chat-visual/p1-rail-dm-avatars.png` — vision-inspected: Alice Nguyen +
  Bob Patel rows show photo avatars with green presence dots (B online in a second session).
- bUnit **264/264**; build 0 warnings.

**Deviations**: none. **Lessons**: the probe login must fill Tenant first (copied from the
canonical probe) — login-by-label without tenant silently never submits.

## P2 — Products list (2026-08-24) — ✅

**What changed**
- `ProductsPage.razor`: brand/category filters are now **searchable comboboxes** (MudAutocomplete,
  type-to-narrow, clearable — React Combobox parity) with `ValueChanged` → server refetch; desktop
  table wrapped in `.fsh-products-desktop d-none d-md-block`; new **mobile card list**
  (`.fsh-prod-cards d-md-none`, React MobileCard parity: image, name + hidden tag, SKU, brand
  chip, category, price, stock chip, always-visible edit, chevron); row actions get
  `.fsh-row-actions` (hover-reveal on desktop, always visible on touch).
- `BrandsPage.razor` / `CategoriesPage.razor` (option A): slug/created cells get
  `.fsh-col-slug`/`.fsh-col-created` (hidden <960px); slug moves under the description via
  `.fsh-slug-mobile`; actions hover-reveal.
- `fsh.css`: hover-reveal rules (+ `hover:none` touch exception), mobile card styles, responsive
  grid collapses for brands/categories, `.fsh-slug-mobile`.

**Evidence**
- `walkthrough/probe-products-p2.mjs` — **11 PASS / 0 FAIL**: combobox narrows on typing;
  actions opacity 0 → 1 on hover; 10 mobile cards render with desktop table hidden; edit always
  visible on mobile; brands+categories slug/created hidden on mobile with slug under description.
- Screenshots (vision-inspected): `desktop-hover.png` (hovered row shows actions, others hide),
  `mobile-cards.png`, `mobile-brands.png`, `mobile-categories.png`.
- bUnit **264/264**; build 0 warnings.

**Deviations**: none. **Lessons**: same-element class selectors need `.a.b`, not `.a .b` — the
probe timeout was a selector bug, not a product bug (verified via body-text dump).

## P3 — Product detail hero + editor dialog (2026-08-24) — ✅

**What changed**
- `ProductDetailPage.razor`: hero rebuilt to React `EntityDetailHero` parity — identity row with
  actions on the RIGHT (was stacked below), subtitle = SKU mono-chip · brand · category, stats row
  of tone-tinted pills (price → price dialog, stock → stock dialog, images count), meta row
  (created/updated relative), `mb-5` card spacing; delete button gets destructive hover.
- `ProductEditorDialog.razor`: React layout parity — Name|SKU 2-col grid (SKU uppercase mono,
  disabled in edit with "fixed after creation" hint), Brand|Category as **searchable
  MudAutocomplete pickers** (Combobox parity), Price|Currency|Stock 3-col (create only),
  visibility switch row (edit) with React copy, footer pending states.
- `fsh.css`: `.fsh-detail-hero-body`, `.fsh-sku-chip`, `.fsh-detail-stat` (+primary/warning/danger
  tones, clickable), `.fsh-detail-meta(-item)`, `.fsh-btn-danger-outline`, `.fsh-dialog-grid-2/3`
  (stack <640px).
- Code-behind: `BrandNameOf/CategoryNameOf/StockTone/StockLabel` helpers; dialog
  `_brandSel/_catSel` + filter funcs.

**Evidence**
- `walkthrough/shot-product-detail.mjs` + `evidence/p3-detail/hero-desktop.png` (vision-inspected:
  gradient strip, avatar tile, title+ACTIVE badge, SKU chip · brand · category, outlined
  Refresh/Edit/Delete right-aligned, stat pills, meta row) + `hero-mobile.png`.
- Regression: `probe-actions.mjs` **37 PASS / 0 FAIL** (product/brand/category CRUD green with
  the new dialog; D12's one flaky run was a devserver cold-start MONO_WASM artifact — focused
  diag showed the page healthy at 26 rows); bUnit **264/264**; build 0 warnings.

**Deviations**: none. **Lessons**: `UserAttributes` needs `Dictionary<,>` not
`IReadOnlyDictionary<,>`; devserver cold-start can emit MONO_WASM download errors that surface as
spurious probe failures — re-run before diagnosing product bugs.

## P5 — Tickets filter bug + render-stall class fix (2026-08-24) — ✅

**Root cause (proven):** every list page used fire-and-forget `_ = LoadAsync()` in click
handlers (filters, pagers, clear-search). The load completed but **nothing re-rendered** — the
UI stayed on the loading skeleton forever. The API was never at fault (all filter values return
200 with correct counts in <60ms — verified via `check-ticket-filters.mjs`). The bug was
invisible until a user clicked a filter; search boxes worked because their debounce path awaits.

**Fix:** converted all 17 fire-and-forget call sites across 8 pages to awaited `async Task`
handlers (event-handler completion triggers the render): TicketsListPage
(SetStatus/SetPriority/GoToPage/ClearFilters), ProductsPage (combobox handlers, visibility
pills, pager, clear), Brands/Categories (pager, clear-search), Users (ClearFilters),
Roles/Groups (clear-search), Invoices + Wallet (pagers).

**Second fix (products):** the filter bar lived inside the results `else` branch — a filter
yielding zero rows hid the pills, making the filter unclearable. Moved above the results
(React products.tsx renders FilterRow unconditionally).

**Verification:** `probe-filters-p5.mjs` **11/0** — tickets Open(8)/Closed(0)/Resolved(2)/
All(13), priority High(2)/Low(2), products Hidden→All restore, brands search+clear; all with
0 skeletons. Full CRUD harness `probe-actions.mjs` **38/0** (probe timing hardened: rowVisible
polls 6s, D12 waits out the WASM cold boot, D17 uses the page search like D19). bUnit **264/264**.

**Lesson:** fire-and-forget data loads in Blazor event handlers are render-stall bugs — the
completion render only happens for awaited handler paths.

## P6 — Dialog standardization (2026-08-25) — ✅

Shared `FshFormDialogHeader` (BlazorShared/Components): icon tone-tile + title + muted
description — the React DialogHeader chrome. Applied to 10 dialogs: product, brand, category,
ticket, group, role (dashboard + admin), user (dashboard), add-group-members, create-channel,
webhook create. One CSS rule outlines the leading Cancel in every `.mud-dialog-actions`
(React DialogFooter parity) — no per-file churn.

**Single-title fix:** MudBlazor renders the ShowAsync title bar AND the in-body header →
double title. Emptied the ShowAsync titles for the 9 scaffolded dialogs (CloseButton X
remains). Gotcha: one edit (ProductsPage) silently didn't land — the diag probe
(`diag-dialog-title.mjs`, dumps `.mud-dialog-title` innerText) caught it; re-applied.

**Verification:** `probe-dialogs-p6` (Cancel border 1px = outlined, 0 console errors, single
title; screenshots editor-product / dialog-ticket / confirm-delete); probe-actions **38/0**;
bUnit **264/264**. Commit `a8798d51`.

**Lesson:** after batch edits, grep-verify each landed — one of nine identical-looking edits
silently missed and cost a rebuild+restart cycle to find.

## P4 — Files page parity — ✅ (2026-08-25)

Side-by-side visual diff (`shot-files-both.mjs`, React 5174 vs Blazor 5176, desktop+mobile),
then reworked `FileManagerPage` to React my-files.tsx parity: header count chip + full
description; MudTabs → pill tabs with count badge; centered compact upload zone (whole area
clickable, allowed-types caption); type filter pills WITH live counts; table →
Filename/Visibility/Size/Uploaded + chevron (Actions column removed — actions live in the
preview, React parity); icon tiles; absolute dates; Public chip → info blue; row click opens
preview.

**Preview-dialog gap found + fixed:** React's preview owns flip-visibility + delete; the
Blazor preview had only Close. Added Make public/private + Delete (owner-gated,
`OnChanged` → page reload). D13's "file deleted" now passes through the preview.

**Probe lesson (headless + Blazor async handler chain):** clicking the zone opens a NATIVE
file chooser that BLOCKS the renderer — Playwright's chooser interception never engages
through the async interop chain, so every later action times out ("performing click action"
hangs; a JS-evaluate click hung the whole script = main-thread block proof). Fix: drive the
hidden `#fileInput` directly via `setInputFiles` (diag-d13.mjs).

**Verification:** probe-actions **39/0**; bUnit **264/264**; visual shots `p4-files/*.png`.

## P7 - Audits filters verification (2026-08-25) - DONE

No product bugs. UI probe probe-audits-p7 10/0 (type/severity/search/hide-activity/range
presets/advanced panel/clear-restores/0 console errors) + API-level verification
(diag-audits-api): every filter returns only matching items - eventType/severity/FromUtc/
ExcludeEventType all 0 mismatches; search matches Source/UserName/PayloadJson by design
(confirmed via detail fetch - summary DTO does not show the payload). The P5 render-stall
class never touched this page (all handlers awaited).

## P8 - Leftovers + close-out (2026-08-25) - DONE

probe-cleanup.mjs: 14 QA channels archived (DELETE = archive), 19 QA roles deleted
(first pass); catalog/files already self-cleaned by the CRUD probes. Accepted leftovers
(documented in plan.md): 16 QA tickets (no public delete endpoint), 2 QA tenants (delete
404s - deactivate-only API), QA users (detail-page flow only). DM toast wording kept
(title semantics match React channelTitle).

Final battery: probe-actions 39/0 - bUnit 264/264 - probe-filters-p5 11/0 -
probe-audits-p7 10/0 - probe-chat-realtime 16/0 - probe-chat-scroll 5/0.
Commits: 564de110 (P1) 411807ab (P2) 4b862c68 (P3) 64db552f (P5) a8798d51 (P6)
6cc1417e (P4) + this docs/cleanup commit.
