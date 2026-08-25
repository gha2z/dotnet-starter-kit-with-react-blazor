# Plan — Phase 09: UI Parity Hardening

> Agent-authored from spec.md. Status markers: ☐ pending · 🔄 in progress · ✅ done (with evidence ref).
> The user confirms this plan before implementation begins.

## Success criteria (overall)

1. Every spec item verified in a real browser: real clicks, CRUD runs, free resizes + device
   emulation (390×844 / 768×1024 / 1440×900), screenshots inspected, zero console errors.
2. Regression battery green: bUnit 264/264, probe-chat-realtime 16/0, probe-actions (dashboard +
   admin), probe-thorough-qa 4-combo.
3. One commit per wave; implementation.md updated per wave with evidence.

## Waves

### P1 — Chat UX (dashboard-blazor) — ✅ (2026-08-24)
- [x] P1.1 Render-aware scroll: `_scrollPending` flag consumed in `OnAfterRenderAsync`; own sends
      always scroll; incoming scrolls only within ~150px of bottom; jump-to-bottom pill when suppressed. — ✅ probe-chat-scroll 5/0
- [x] P1.2 DM rail: partner avatar (photo → initials fallback) + green presence dot (reuse
      `IsUserOnline` + partner from `ChannelDto.Members`). — ✅ p1-rail-dm-avatars.png
- [x] P1.3 Re-verify: probe-chat-realtime 16/0 + scroll assertions; rail screenshots desktop+mobile. — ✅ 264/264 bUnit

### P2 — Products list — ✅ (2026-08-24)
- [x] P2.1 Searchable combobox filters (brand/category) — type-to-narrow, clearable, React filter
      variant styling; reuse in ProductEditorDialog. — ✅ (list filters done; editor pickers land with P3.2)
- [x] P2.2 Row actions: hover-reveal edit/delete (desktop), always visible (mobile). — ✅
- [x] P2.3 Responsive columns: Brands + Categories lists hide slug/created on mobile, slug under
      description (option A); Products keeps React's exact column set. — ✅
- [x] P2.4 Mobile card list (React MobileCard parity). — ✅
- [x] P2.5 Visual verification desktop + mobile. — ✅ probe-products-p2 11/0 + screenshots

### P3 — Product detail hero + catalog dialogs — ✅ (2026-08-24)
- [x] P3.1 Hero → EntityDetailHero parity (avatar tile, title + Active/Hidden badge, SKU chip ·
      brand · category subtitle, outline Refresh/Edit/Delete right-aligned, stat pills, meta row,
      padding/rhythm, images↔Browse gap). — ✅ shot-product-detail + vision inspection
- [x] P3.2 ProductEditorDialog to React layout (2-col name/SKU, searchable pickers, 3-col
      price/currency/stock create-only, visibility switch row, pending footer). — ✅
- [ ] P3.3 Brand/Category editor dialogs restyle — folded into P6 (shared scaffold). — 🔄 moved

### P4 — Files page parity — ✅ (2026-08-25)
- [x] P4.1 Side-by-side visual diff (:5176 vs :5174) desktop+mobile → delta list. — ✅
- [x] P4.2 Fixed deltas: header count chip, pill tabs w/ badge, centered upload zone, type
      pills with counts, chevron rows (Actions → preview), icon tiles, absolute dates, blue
      Public chip; **preview dialog gained flip-visibility + delete (React parity)**; real
      upload/preview/delete verification. probe-actions 39/0. — ✅

### P5 — Tickets: filter bug + filters look — ✅ (2026-08-24)
- [x] P5.1 Diagnosed: API was never at fault (all filters 200, <60ms) — root cause was the
      fire-and-forget `_ = LoadAsync()` render-stall pattern; fixed across **8 pages / 17 call
      sites**. — ✅
- [x] P5.2 Filter pills verified against React (tickets pills were already parity); products
      filter bar moved above results (was hidden on zero-row results — unclearable). — ✅
- [x] P5.3 `probe-filters-p5.mjs` **11/0** (every ticket status/priority value + products
      visibility + brands search/clear). — ✅

### P6 — Dialog standardization (all CRUD) — ✅ (2026-08-25)
- [x] P6.1 Shared `FshFormDialogHeader` (icon tile + title + description) in BlazorShared +
      CSS chrome; leading Cancel outlined app-wide via one `.mud-dialog-actions` rule. — ✅
- [x] P6.2 Refactored onto the header: product, brand, category, ticket, group, role (dash+admin),
      user (dash), add-members, channel, webhook dialogs; ShowAsync titles emptied so the body
      header is the single title (React parity); delete confirms unchanged (already consistent). — ✅
      Verified: probe-dialogs-p6 (Cancel 1px outlined, 0 console errors, single title) +
      probe-actions 38/0 + bUnit 264/264.

### P7 — Audits filters verification — ✅ (2026-08-25)
- [x] P7.1 UI probe (probe-audits-p7 **10/0**: type/severity/search/hide-activity/range presets/
      advanced panel/clear-restores/0 console errors) + API-level verification
      (diag-audits-api: every filter returns only matching items — eventType/severity/FromUtc/
      ExcludeEventType all 0 mismatches; `search` matches Source/UserName/**PayloadJson** by
      design, confirmed via detail fetch). No product bugs — the P5 render-stall class never
      touched this page (all handlers awaited). — ✅

### P8 — Leftovers + close-out — ✅ (2026-08-25)
- [x] P8.1 QA-entity cleanup probe (`probe-cleanup.mjs`): **14 QA channels archived**,
      19 QA roles deleted (first pass); catalog/files already self-cleaned by the CRUD
      probes. Accepted leftovers (documented): 16 QA tickets (no public delete endpoint),
      2 QA tenants (delete 404s — deactivate-only API), QA users (detail-page flow only).
      DM toast wording kept as-is (title semantics match React's channelTitle). — ✅
- [x] P8.2 Full regression battery green (probe-actions 39/0, bUnit 264/264, probe-filters
      11/0, probe-audits 10/0, probe-chat-realtime 16/0, probe-chat-scroll 5/0); per-wave
      commits; STATUS.md dashboard + implementation.md current. — ✅

## Execution order

P1 → P2 → P3 → P6 → P4 → P5 → P7 → P8. Stack restarts agent-driven. One commit per wave.
