# HUMAN-GUIDE — FullStackHero .NET Starter Kit, for humans

> This is the people's-eye view of the agentic workflow that runs on this repo.
> AI agents read `AGENTS.md` + `workflows/{current,neutral}/session-protocol.md`.
> Humans read this.
> **Last updated: 2026-08-25 (workflow streamlining; single-source-of-truth cleanup).**

## What this repo carries besides code

This repo is not just a .NET solution — it is also the **home** of a reusable, agentic
SaaS-development workflow. The workflow is **repo-local**: what drives coding sessions on
*this* repo, organized as **tracks** (below).

To seed a new project, use `opencode/init-saas-workflow.ps1` (it clones **this** repo as
its source). The script copies the entire workflow — skills, rules, protocol, template — into
your new project so it works out of the box with any tool (opencode, Claude Code, Copilot, etc.).

## Where the workflow source lives

| Path | What it is | Who maintains it |
|---|---|---|
| `AGENTS.md` | **Entry point** for all tools — the canonical guide | this repo |
| `workflows/current/` | **OpenCode package** — session protocol, coordination, task-skills, verify | this repo |
| `workflows/neutral/` | **Neutral package** — same protocol, no tool dependencies (Claude Code, Copilot, etc.) | this repo |
| `workflows/_tracks-template/` | Reusable **track scaffold** — start a new work-stream from here | this repo |
| `.agents/skills/` | **Task recipes** — step-by-step guides for specific tasks (add-feature, add-blazor-page, etc.) | this repo |
| `.agents/rules/` | **Conventions** — architecture, database, API, testing, frontend rules per area | this repo |
| `docs/spec/` | The **requirements home** — one spec file per app/stream | this repo (you author) |

**Mental model:** the workflow is authored **here** and used **here**. `.agents/` (skills, rules,
workflows) at the repo root is the single source of truth — there is no second copy anywhere.
Any improvement is made **here** and committed here.

## Tracks — how this workflow scales to many apps

A **track** is one work-stream with its own owner, zone, roadmap, and status:

> **New track = new owner + disjoint zone + (spec | index | status | live).**
> If any of those isn't real, it's a **phase**, not a track.

| Track | App / stream | Owner | Status |
|---|---|---|---|
| `opencode/addBlazorFrontends/` | React→Blazor→MAUI **parity** | you | **active** |
| `opencode/erp/` (future) | Client back-office | your clients | not started |
| `opencode/pos/` (future) | Store POS | your clients | not started |

To start a new track: run `opencode/init-saas-workflow.ps1` for a new project, or
copy `workflows/_tracks-template/` → `opencode/<name>/` for a new track in this repo.

## Scaffolding a new SaaS project

The bootstrap script creates a complete, production-ready project from this codebase:

```powershell
# Minimal (interactive prompts):
pwsh opencode/init-saas-workflow.ps1 -Name AcmeSaaS -WorkDir C:\dev\AcmeSaaS

# Full options:
pwsh opencode/init-saas-workflow.ps1 -Name AcmeSaaS -WorkDir C:\dev\AcmeSaaS `
    [-FromClone <path>]              # source repo (default: this repo)
    [-StripReact]                    # drop React admin + dashboard clients
    [-StripMaui]                     # drop MAUI Hybrid app
    [-StripModules Catalog,Billing]  # drop specific backend modules
    [-SkipClone]                     # source already scaffolded
    [-SkipBuild]                     # skip restore + build gate
    [-NoCommit]                      # skip initial git commit
    [-NonInteractive]                # accept all defaults, no prompts
```

**What the script does (7 steps):**

1. **Copy** — clones the repo (excludes .git, bin/obj, node_modules, .swarm)
2. **Rename** — `FSH.*` → `<Name>.*` everywhere (files, folders, content)
3. **Strip** — optionally removes React clients, MAUI app, or specific modules
4. **Clean** — removes project-private dirs (active tracks, temp, swarm state)
5. **Wire** — creates `.opencode/` config, skill-routing, updates .gitignore
6. **Build gate** — `dotnet restore` + `dotnet build` (must pass 0 errors)
7. **First commit** — `git init` + initial commit

**What you need to provide:**
- `-Name`: a valid C# identifier (e.g., `AcmeSaaS`, `MyPlatform`)
- `-WorkDir`: where to create the project (default: `./<Name>` in current directory)
- Decide which modules to keep (Catalog, Chat, Tickets, etc.)
- Decide which frontends to keep (React, Blazor, MAUI)

**What you get after bootstrap:**
```
AcmeSaaS/
├── AGENTS.md                    ← canonical guide (renamed, references updated)
├── HUMAN-GUIDE.md               ← this file (renamed)
├── CLAUDE.md / GEMINI.md        ← tool bridges (renamed)
├── .agents/                     ← skills, rules, workflows (ready to use)
├── .opencode/                   ← opencode config (swarm + skill routing)
├── workflows/                   ← session protocol, coordination, verify
│   ├── current/                 ← opencode package
│   ├── neutral/                 ← neutral package
│   └── _tracks-template/        ← track scaffold
├── opencode/
│   ├── init-saas-workflow.ps1   ← reusable bootstrap script
│   ├── AGENTIC-GUIDE.md         ← human + agent guide (renamed)
│   └── AcmeSaaS/                ← first track (seeded from template)
│       ├── 00-Index.md           ← roadmap
│       ├── STATUS.md             ← dashboard
│       ├── zones.md              ← ownership map
│       ├── live/                 ← session artifacts
│       └── 00_summary/           ← wave evidence
├── docs/spec/                    ← requirements home
├── src/                          ← .NET solution (renamed FSH → AcmeSaaS)
├── clients/                      ← React + Blazor + MAUI apps
└── ...
```

## How you drive it — the six verbs

You stay the reviewer/approver; agents plan, build, and verify:

1. **Spec** — write plain-language requirements in `docs/spec/` (one file per app).
2. **Plan** — say *"plan the next phase of <track> against docs/spec/<file>.md"* → the agent
   produces `Phase-NN-*/plan.md` with `FR-###` ids traced to your Musts.
3. **Approve** — you okay the plan. No plan, no code.
4. **Build** — say *"build FR-00x"* → one gated loop: coder → reviewer → verify gate.
5. **Approve stage** — agent stages by explicit path, shows diff, **you** approve the commit.
6. **Audit** — periodically *"run the zero-gaps audit"* → agent produces gap table, then plan next phase.

## Two operating modes

| Mode | Best for | TL;DR |
|---|---|---|
| **Swarm** (single-session) | One cohesive chunk: a page, feature, or fix | architect → coder → reviewer → verify. All in one opencode session. |
| **Manual multi-session** | Parallel work on disjoint zones | Multiple `sess-<id>` lanes coordinated via `coordination.ps1` |

You don't need to learn either — the agents do. You need to know:

- **Nothing gets committed without your say-so.** Agents stage by explicit path and ask.
- **`git push` is never done by an agent** unless you explicitly direct it.
- **Verification is scripted and non-negotiable**: build, tests, Playwright walkthrough.
- **Docs travel with the change** (Golden Rule 10).

## The ritual is self-starting

The workflow is written so agents **automatically run the session ritual** — you give it
a task and it does the rest. The single source of truth for the ritual is:

- **OpenCode:** `workflows/current/session-protocol.md`
- **Neutral tools:** `workflows/neutral/session-protocol.md`

If you see an agent skipping steps, point it at the protocol file rather than re-explaining.

**What the ritual does (summary):**
1. On any task: reads `AGENTS.md` → protocol → track's `STATUS.md` → `live/*.md` (never from memory)
2. Every turn: stamps heartbeat
3. Before staging: takes verify lock, runs gate, shows diff for your approval
4. Wave close-out: summary + STATUS append + board update
5. Verification numbers always come from a real run this session

## Golden Rules that protect you (and the codebase)

These are enforced by `Architecture.Tests` and by the workflow:

1. Module boundaries — modules talk only through `.Contracts`.
2. Registering a module touches **four** places.
3. Tenant isolation is default-ON; opt out only via `IGlobalEntity`.
4. Do NOT modify `src/BuildingBlocks` without explicit approval.
5. Handlers: `public sealed`, `ValueTask<T>`, `.ConfigureAwait(false)`.
6. Structured logging only.
7. Propagate `CancellationToken`.
8. Every command/paginated-query handler needs a validator.
9. Frontend: pass per-call data through `mutate(arg)`, never captured state.
10. Docs + changelog travel with the change.

## Tool-specific setup

| Tool | How it connects | Bridge file |
|---|---|---|
| **OpenCode** | Reads `AGENTS.md` → `workflows/current/` package. Swarm config in `.opencode/`. | `.opencode/opencode.json` + `opencode/AGENTIC-GUIDE.md` |
| **Claude Code** | Reads `AGENTS.md` → `workflows/neutral/` package. | `CLAUDE.md` (thin bridge) |
| **GitHub Copilot** | Reads `AGENTS.md` → `workflows/neutral/` package. | `AGENTS.md` (direct) |
| **Gemini CLI** | Reads `AGENTS.md` → `workflows/neutral/` package. | `GEMINI.md` (thin bridge) |
| **Kimi / DeepSeek** | Reads `AGENTS.md` → `workflows/neutral/` package. | `AGENTS.md` (direct) |

All tools share the same `.agents/` knowledge base (skills, rules, workflows).
The only difference is which `workflows/` package the agent reads.

## If something looks wrong

- **A model identity is wrong** → re-verify from the fresh session (readme.md → "Model Identity Convention").
- **A requirement is missing from a plan** → it should be a Must in `docs/spec/`, then a `FR-###` in the plan.
- **Anything else** → open an issue on this repo.

---

*Full detail for agents: `AGENTS.md` → `workflows/{current,neutral}/session-protocol.md`.
Requirements: `docs/spec/`. New project: `opencode/init-saas-workflow.ps1`.*
