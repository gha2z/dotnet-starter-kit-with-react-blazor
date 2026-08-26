# <track-name>

> Status:  [scaffolded, ready for planning] | **Owner:** _you_

## 1. Recommended starting order

1. Decide what you need — add, extend, or fix.
2. Write requirements in `docs/spec/NNN-<track-name>.md` (one file per app/stream).
3. **Plan** — tell the agent: *"plan the next phase of <track> against docs/spec/<file>.md"*
4. Agent creates `Phase-NN-<Mission>/plan.md` with tasks and success criteria.
5. **Approve** — you okay the plan. No plan, no code.
6. **Build** — agent implements per the plan; you review wave close-outs.
7. **Audit** — periodically *"run the zero-gaps audit"* to compare parity.

## 2. Files this track carries

| File/dir | Purpose | Owner |
|---|---|---|
| `00-Index.md` | Roadmap — phase list with status | you + agent |
| `STATUS.md` | Top dashboard: current mission, next task, obstacles; append-only ledger below | agent |
| `readme.md` | Track-specific reference: scope restriction, subagent dispatch, DoD, dev servers | you |
| `zones.md` | Session→file-ownership map (parsed by `coordination.ps1`) | you |
| `plan.md` | (root-level, optional) High-level planning across phases | agent |
| `live/sess-*.md` | Per-session working state (identity, heartbeat, current focus, touched files) | agent |
| `live/board.md` | Append-only rows for cross-session coordination | agent |
| `live/lessons.md` | Shared lesson ledger; top section = failure-pattern registry | agent |
| `live/locks/verify.lock` | Exclusive lock while verify/build scripts run | agent |
| `00_summary/` | Wave evidence — verification summaries, probe output, screenshots, diffs | agent |
| `Phase-NN-<Mission>/` | One folder per planning cycle: `spec.md`, `plan.md`, `implementation.md` | agent |
| `walkthrough/` | (optional) Playwright probes and E2E evidence | agent |

## 3. Operating rules (always)

- **Coords** = `coordination.ps1 -TrackRoot <path-to-this-dir>` (not the track's `coordination.ps1`)
- **Verify** = `pwsh <track-dir>/verify.ps1` (or the default `workflows/_tracks-template/verify.ps1`)
- Verify = Clean build + full tests, always from a clean state.
- Do **not** run verify and build in parallel.
- Stage only explicit paths; never auto-commit; never auto-push.
- Numbers in `STATUS.md` and summaries come from real runs this session only.
- **Route compliance is checked by the coordination gate** (zones.md + heartbeat).

## 4. Starting work

- **FROZEN REACT PAIR:** `clients/admin` + `clients/dashboard` are read-only reference standards.
  All new work targets the Blazor WASM twins (`clients/admin-blazor`, `clients/dashboard-blazor`)
  and the MAUI Hybrid (`clients/FSH.Hybrid`). See AGENTS.md Golden Rule 11.
- New work must target **parity to zero gaps** vs the React pair or **enhancing** the Blazor/Hybrid UX beyond what React offers (e.g. native MAUI capabilities).
- Pages must validate via **real-browser Playwright walkthrough** (in `walkthrough/`) — bUnit is necessary but not sufficient.
- Pages and functionality must match in both Blazor WASM apps.
- Playwright scripts must verify visual layout, interactivity, console clean, and network clean.
- New page root = Blazor WASM BFF; route same as original React app (see `walkthrough/011-auth-register-refactor-probe.md` for conventions).
