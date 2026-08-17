# Enhanced Workflow Packages

> Two self-contained copies of the enhanced agentic workflow (session protocol + coordination +
> self-improvement + progress tracking), consolidated so every ritual is **self-starting** and
> every session can resume from **on-disk state** — even after context compaction or a brand-new
> session with a different tool.

The session protocol is written once, here, and referenced everywhere else. The legacy scatter
(`opencode/AGENTIC-GUIDE.md` §2.1/§5, `opencode/addBlazorFrontends/readme.md`,
`WORKFLOW-GUIDE.md`, `HUMAN-GUIDE.md`) is being slimmed to point here instead of repeating it.

## The two packages

| Package | Folder | Environment | Tool bindings |
|---|---|---|---|
| **1. Current environment** | `workflows/current/` | This repo (Blazor WASM twins + MAUI Hybrid + React pair) | opencode + opencode-swarm + dotnet-skills plugin suite |
| **2. Neutral (original repo)** | `workflows/neutral/` | `github.com/fullstackhero/dotnet-starter-kit` (React pair only) | Any tool: GitHub Copilot (VS 2026 / VS Code), Claude Code, Cursor, Gemini CLI… no opencode plugins, no dotnet-skills suite |

Both packages share the same protocol core — the qualities that make multi-session agentic
development survive context limits:

- **Self-starting ritual** — the ritual is invoked by the agent itself on every task; no human
  reminder ("follow the ritual") is ever needed.
- **On-disk state only** — STATUS ledger, session files, summaries, lessons ledger, board, zones.
  A session never reconstructs progress from memory; it reads the disk and continues. This is the
  compaction-proof guarantee.
- **Session coordination** — heartbeat, verify lock, explicit-path staging, ownership zones parsed
  from `zones.md` (single authority — no more script/map drift).
- **Self-improvement** — the lessons ledger (one line per turn) + failure-pattern registry; the
  staging gate fails closed while the ledger is missing.
- **Progress visibility** — one-line STATUS ledger + wave summaries + Kanban board → humans and
  any future session see exactly where the work stands.

## Install in 5 minutes

1. **Pick a package** — `current/` for this repo, `neutral/` for a copy of the original repo
   (or any clean React-only clone you want to test on Copilot/Claude Code/…).
2. **Copy the package** into the repo root (the docs assume `<repo>/workflows/<package>/`; adjust
   the `coordination.ps1 -TrackRoot` argument if you place it elsewhere).
3. **Wire the ritual** — paste `adapt/AGENTS-session-protocol.md` into the repo's `AGENTS.md`
   (neutral package also ships a `CLAUDE.md` bridge). Paths in the snippet are placeholders —
   resolve them to your package location.
4. **Seed a track** — copy `tracks/_template/` (neutral) or use the repo's existing track /
   `opencode/_tracks-template/` (current) to create your first work-stream: `opencode/<track>/`
   with `STATUS.md`, `00-Index.md`, `zones.md`, `live/`.
5. **Open the first session and give one task** — the agent self-starts the ritual, heartbeats,
   works, verifies, and closes out with a summary + lessons line. You review the staged diff.

## De-duplication ledger (what was consolidated)

| Duplicated across | Now lives in |
|---|---|
| Session start ritual (4+ places) | `session-protocol.md` §1 (per package) + `adapt/AGENTS-session-protocol.md` |
| Task→skill map (3 places) | `task-skills.md` (per package) |
| Coordination rules 1–13 (3 places) | `session-protocol.md` §2 (per package) |
| QA/verification contract (3 places) | `verify.md` (per package) |
| Zone→owner map (script + docs drift) | `zones.md` — parsed by `coordination.ps1` v2 (single authority) |
| New-project bootstrap (3 places) | package `README.md` + (current) `opencode/init-saas-workflow.ps1` |

## Files in this repo that remain authoritative

- Live track state (this repo): `opencode/addBlazorFrontends/` — **do not edit**; the packages
  document it, they do not replace it.
- Rules/skills/workflows catalog: `.agents/` (indexed from `AGENTS.md`).
- Requirements: `docs/spec/` (this repo).