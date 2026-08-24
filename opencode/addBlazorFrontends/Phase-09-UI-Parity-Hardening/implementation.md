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
