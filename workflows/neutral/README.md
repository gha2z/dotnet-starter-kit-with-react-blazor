# Enhanced Workflow — Neutral Package

> A full copy of the enhanced agentic workflow with **zero opencode / plugin couplings** — only
> PowerShell, git, markdown files, and dotnet/npm CLI. Built for the **original**
> `fullstackhero/dotnet-starter-kit` repo (React pair only) and safe to test on **any** coding
> tool: GitHub Copilot (VS 2026 / VS Code), Claude Code, Cursor, Gemini CLI, and others —
> they all read a root `AGENTS.md` (and `CLAUDE.md` for Claude Code) and execute shell commands.
>
> **Skill note (verified):** the original repo already ships its full `.agents/` ecosystem —
> `.agents/rules/`, `.agents/skills/*/SKILL.md` (19 skills), `.agents/workflows/` — documented in
> its `AGENTS.md`. So this package intentionally uses **only** those FSH skills (read as plain
> markdown) and does **not** include the opencode-only dotnet-skills plugin suite.

## Install (original repo, ~5 minutes — the agent does the rest)

Topology shown in the snippets: package at `workflow/neutral/`, work-stream at `workflow/<track>/`.
You may rename both folders — the only path coupling is the `-TrackRoot` default at the top of
`coordination.ps1` and the paths inside `adapt/`; resolve all `<workflow-dir>` placeholders.

This README is for the human installer only — **agents do not need to read it.** At runtime an
agent reads `session-protocol.md` (the ritual), `task-skills.md` (the skill map), and the
track's own docs (`STATUS.md`, `live/*.md`). Everything after step 2 is mechanical and is
self-performed by the first agent.

```bash
# 1. copy the package
cp -r workflows/neutral <repo>/workflow/neutral

# 2. wire the ritual into the repo's instruction files
#    (GitHub Copilot in VS 2026 / VS Code, Cursor, Gemini CLI, Codex → AGENTS.md)
#    paste workflows/neutral/adapt/AGENTS.md into <repo>/AGENTS.md (resolve <workflow-dir>)
#    (Claude Code → additionally copy adapt/CLAUDE.md to <repo>/CLAUDE.md)
```

Steps 1–2 are the **only human steps**. Do not seed the track yourself.

```bash
# 3. first task → give the agent: "Seed the work-stream: copy tracks/_template to
#    workflow/track-main, resolve the <track>/<sid> placeholders in its *.md, then start
#    the session ritual (session-protocol.md §1)." The agent performs every step, including
#    the first coordination.ps1 -Session <sid> -Start.

# 4. from then on: new sessions self-start the ritual on every task — nothing to run by hand.
```

(Optional, for reference — the seed steps the agent runs: in `tracks/_template/zones.md` replace
the owner placeholder with your session id; replace `<track>` / `<sid>` in `*.md`. `track-main`
is just the **default work-stream name**, not a phase — parallel work-streams pick their own,
e.g. `track-maui`, and the template is copied once per work-stream.)

`coordination.ps1` runs on Windows (pwsh) and via `pwsh` on Linux/macOS; no other tooling needed.

## Package layout

| Path | What |
|---|---|
| `session-protocol.md` | The protocol core — same ritual, coordination, close-out and self-improvement as the current-environment package, written tool-neutral |
| `coordination.ps1` | Mechanical gate v2 (heartbeat / lock / zones / lessons / gate / close-out) — zones parsed from the track's `zones.md`; `pwsh` only |
| `task-skills.md` | Task→skill map for the original repo — `.agents` FSH skills only |
| `verify.md` + `tracks/_template/verify.ps1` | Verification contract + the neutral verify script (dotnet + npm + Playwright) |
| `adapt/AGENTS.md` | The exact self-starting ritual section for the original repo's `AGENTS.md` |
| `adapt/CLAUDE.md` | Bridge file for Claude Code (points at `session-protocol.md`) |
| `tracks/_template/` | The full work-stream scaffold: `STATUS.md`, `00-Index.md`, `zones.md`, `plan.md`, `00_summary/_template.md`, `live/` (README, board, session template) |

## The quality set this package preserves

1. **Self-starting ritual** — runs because the agent read `AGENTS.md`, never because a human asked.
2. **On-disk state** — STATUS ledger + session files + summaries + lessons ledger. Context
   compaction / new tool / new session ⇒ read the disk, continue. Progress is visible to humans
   at all times (`STATUS.md` one-liner, `live/board.md`, summaries).
3. **Coordination without a server** — heartbeats, verify lock, zone ownership (`zones.md`),
   claim lines in `plan.md`; single-checkout multi-session safe via explicit-path staging.
4. **Self-improvement** — one lesson line per turn; failure-pattern registry; gate fails closed
   while the ledger is missing.
5. **Evidence over memory** — verification numbers from real runs this session; model identity
   re-verified every session.

## What changed vs the current-environment package

| Aspect | current/ | neutral/ |
|---|---|---|
| Tool bindings | opencode + swarm + dotnet-skills suite | none — any tool |
| Memory tools | `ctx_memory` / `ctx_search` / `knowledge_recall` (recall aids) | on-disk files are the entire memory; native tool memory optional, never authoritative |
| Skills | FSH recipes + dotnet plugin suite | FSH `.agents` skills only (read as markdown) |
| Frontends | Blazor twins + MAUI Hybrid + React pair | React pair only |
| Skill loading | `skill` tool | read `SKILL.md` files directly |