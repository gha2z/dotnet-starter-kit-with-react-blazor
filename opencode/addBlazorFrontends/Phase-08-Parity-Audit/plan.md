# Phase-08 Parity Audit — Follow-up Build Plan (READ-ONLY proposal)

Companion to `audit.md`. This is a **proposed** next-build-phase queue, cost-ordered.
Phase-08 made no source changes; this file only records what a follow-up build phase
would do. Decisions belong to the next build session + user sign-off.

## Goals

1. Close the 4 dashboard-blazor deltas (2 P1, 1 P2, plus expiry banner) — biggest user-visible surface.
2. Make a routing/nav decision for FSH.Hybrid (implement vs gate) — the single structural item.
3. Close the 5 admin-blazor deltas (all P2/P3, low risk).
4. Re-run bUnit suites + this audit's evidence as the parity regression gate.

## Proposed work items (from audit §6 queue)

| # | App zone | Route/page | Work | Sev | Est |
|---|---|---|---|---|---|
| Q1 | dashboard-blazor | `/chat` | `@page "/chat/{Id}"` deep link + auto-select fallback + browser-nav per channel | 🟠 | M |
| Q2 | dashboard-blazor | `/settings/appearance` | Port accent presets + custom-accent dialog + font family + density + motion from React `appearance.tsx` | 🟠 | M |
| Q3 | dashboard-blazor | shell | Port `ExpiryBanner` (grace/expired/≤7d) into `MainLayout` above content | 🟠 | S |
| Q4 | dashboard-blazor | `/system/audits` | Advanced filters (source/user/correlation/trace/tags) + detail drawer related-events timeline + payload copy | 🟡 | M |
| Q5 | admin-blazor | `/audits` | Modal → side-sheet presentation mirror (optional; payload/correlation copy already present) | 🟢 | S |
| Q6 | admin-blazor | `/tenants/{Id}` | 2s provisioning poll + ActiveGrantsCard + TenantBrandingCard + ImpersonateDialog | 🟡 | M |
| Q7 | FSH.Hybrid | nav | DECISION GATE: (a) gate nav to implemented routes, or (b) implement missing `@page` pages to match `NavSpec.cs` | 🟠 | S–XL |
| Q8 | admin-blazor | `/reset-password` | Port `scorePassword` strength meter + STRENGTH_META bar | 🟢 | S |

## Suggested phase order (dependencies + value)

- **Phase 8A (dashboard)** — Q1 → Q2 → Q3 → Q4. Independent pages, serial by convention.
- **Phase 8B (hybrid)** — Q7 alone, requires user decision (gate vs implement) before any work.
- **Phase 8C (admin)** — Q6 → Q8 → Q5 (optional). Independent of 8A/8B.

## Regression gate for any follow-up phase

- `dotnet test` dashboard-blazor bUnit (204/204 baseline) + admin-blazor bUnit (158/158 baseline).
- Re-check each touched route against the React reference cited in `audit.md` §3.
- No edits to `src/BuildingBlocks` (protected).

## Hybrid decision framing (Q7)

- `NavSpec.cs` already mirrors the full dashboard route set; only `/`, `/files`, `/login` exist as `@page`.
- Option (a) gate: quick, honest UX (hide 404 items), shrinks the surface.
- Option (b) implement: full parity but large — the ~18 missing routes are essentially the dashboard-blazor
  pages re-hosted in the Hybrid shell, which is a multi-wave effort.
- Recommendation: **gate first (a), then implement progressively (b)** as the Hybrid stream matures.
