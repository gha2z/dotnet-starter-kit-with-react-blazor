# HUMAN-GUIDE — FullStackHero .NET Starter Kit, for humans

> This is the people's-eye view of the agentic workflow that runs on this repo.
> AI agents read `AGENTS.md` + `opencode/AGENTIC-GUIDE.md`. Humans read this.
> **Last updated: 2026-08-11 (track model + requirements home; standalone `workflow/` machinery retired).**

## What this repo carries besides code

This repo is not just a .NET solution — it is also the **home** of a reusable, agentic
SaaS-development workflow. The workflow is **repo-local**: what drives coding sessions on
*this* repo, organized as **tracks** (below). There is no standalone workflow repository —
the `workflow/` publish/sync machinery was **retired by user decision** (2026-08-11).

To seed a new project, use `opencode/init-saas-workflow.ps1` (it clones **this** repo as
its source), or copy the protocol core by hand: `opencode/AGENTIC-GUIDE.md`,
`opencode/_tracks-template/`, and this file are stack-neutral and portable.

## Where the workflow source lives

| Path | What it is | Who maintains it |
|---|---|---|
| `opencode/AGENTIC-GUIDE.md` | This repo's working guide (agents + humans) | this repo |
| `opencode/addBlazorFrontends/` | The **active track** — React→Blazor→MAUI parity work. Session protocol core (readme, coordination, verify, STATUS, live), roadmap + gap tables. | this repo |
| `opencode/_tracks-template/` | Reusable **track scaffold** — start a new work-stream (a new app) from here instead of copying a whole live track | this repo |
| `docs/spec/` | The **requirements home** — one spec file per app/stream; sessions plan phases off these files | this repo (you author) |

**Mental model:** the workflow is authored **here** and used **here**. `.agents/` (skills, rules,
workflows) at the repo root is the single source of truth — there is no second copy anywhere.
`docs/spec/` is live-repo-only (per-project). Any improvement is made **here** and committed here;
nothing is republished elsewhere.

## Tracks — how this workflow scales to many apps

A **track** is one work-stream with its own owner, zone, roadmap, and status. The rule of thumb:

> **New track = new owner + disjoint zone + (spec | index | status | live).**
> If any of those isn't real, it's a **phase**, not a track.

| Track | App / stream | Owner | Status today |
|---|---|---|---|
| `opencode/addBlazorFrontends/` | React→Blazor→MAUI **parity to zero gaps** | you | **active — main goal** |
| `opencode/erp/` (future) | Client back-office (ERP/CRM/SCM) | your clients | not started |
| `opencode/pos/` (future) | Standalone / integrated store POS | your clients | not started |
| `opencode/consumer/` (future) | Retail mobile app (Play/App Store) | consumers | not started |
| `opencode/operator/` (future) | **You** managing tenants + subscriptions | you | not started |

Each track reuses the same protocol core; zones keep sessions from colliding when tracks run in
parallel. To start a new track: copy `opencode/_tracks-template/`, rename to `opencode/<name>/`,
write `docs/spec/NNN-<name>.md`, declare zones, and add a row above.

## How you drive it — the six verbs

You stay the reviewer/approver; agents plan, build, and verify. This is the whole game:

1. **Spec** — write plain-language requirements in `docs/spec/` (one file per app). This is the only
   place you author requirements.
2. **Plan** — say *"plan the next phase of <track> against docs/spec/<file>.md"* → the architect
   produces `Phase-NN-*/plan.md` with `FR-###` ids traced to your Musts.
3. **Approve** — you okay the plan. No plan, no code.
4. **Build** — say *"build FR-00x"* → one gated loop: coder → reviewer → test engineer → verify gate.
   One session = one feature, never a whole phase.
5. **Approve stage** — I `git add <explicit paths>`, show `git diff --cached --stat`, **you** approve
   the commit. Never auto-committed, never pushed by me.
6. **Audit** — periodically *"run the zero-gaps audit"* → I produce the gap table, then we plan the
   next phase.

## Two operating modes (agents use, humans should be aware of)

| Mode | Best for | TL;DR |
|---|---|---|
| **Swarm** (single-session, gated) | One cohesive chunk: a page, feature, or fix | architect plans → critic gates the plan → coder implements → reviewer + test_engineer gate the result. All in one opencode session. |
| **Manual multi-session** (parallel lanes) | Emulator/device work (MAUI), long E2E, two disjoint zones at once | `sess-main` + `sess-maui` lanes coordinated via `coordination.ps1` heartbeat / locks / gates |

You don't need to learn either to use the repo — the agents do. You need to know:

- **Nothing gets committed without your say-so.** Agents stage by explicit path and ask.
- **`git push` is never done by an agent** unless you explicitly direct it.
- **Verification is scripted and non-negotiable**: `verify.ps1` (both WASM apps), `verify-hybrid.ps1` (MAUI), `dotnet build src/FSH.Starter.slnx` (backend).
- **Docs travel with the change** — a user-facing change isn't done until the docs repo + changelog are updated (Golden Rule 10).

## Golden Rules that protect you (and the codebase)

These are enforced by `Architecture.Tests` and by the workflow. Flag any agent that tries to break them:

1. Module boundaries — modules talk only through `.Contracts`.
2. Registering a module touches **four** places (two `Program.cs` files × Mediator markers + assemblies array).
3. Tenant isolation is default-ON; opt out only via `IGlobalEntity`.
4. Do NOT modify `src/BuildingBlocks` without explicit approval.
5. Handlers: `public sealed`, `ValueTask<T>`, `.ConfigureAwait(false)`.
6. Structured logging only.
7. Propagate `CancellationToken`.
8. Every command/paginated-query handler needs a validator.
9. Frontend: pass per-call data through `mutate(arg)`, never captured state.
10. Docs + changelog travel with the change.

## If something looks wrong

- **A model identity is wrong in a doc** → never copy it forward; re-verify from the fresh
  session (readme.md → "Model Identity Convention").
- **A requirement is missing from a plan** → it should be a Must in `docs/spec/`, then a `FR-###`
  in the plan. Traceability is the contract.
- **Anything else** → open an issue on this repo.

---

*Full detail for agents: `AGENTS.md` + `opencode/AGENTIC-GUIDE.md`. Requirements: `docs/spec/`.
New project: `opencode/init-saas-workflow.ps1` (or copy the protocol core by hand).*