# Workflow Guide — Orchestration Modes + QA Gates

> This file explains the **two orchestration modes** and **QA gates** for this track.
> Read `readme.md` first for protocol overview.

---

## 1. Task → Skill Map

> **Canonical: `workflows/current/task-skills.md`**. Load the skill before the task, not during.
> FSH skills are authoritative for structure; global dotnet-skills inform technique.

---

## 2. Two-Mode Orchestration

### A. Swarm Mode (opencode-swarm plugin)

**Requirements**: `.opencode/opencode-swarm.json` with `parallelization_enabled: true` + plugins installed.

```bash
# Phase flow
/swarm epic 1              # Decide phase 1 (promote/demote based on coupling)
/swarm wave 1              # Dispatch wave 1 (parallel, one coder per disjoint task)
/swarm wave 2              # ... subsequent waves
/swarm review 1            # reviewer + test_engineer gate (or full 5-member council)
/swarm complete 1          # Phase complete: drift verify + evidence write
```

**Agent roster (lean default)**: architect, coder, explorer, reviewer, test_engineer
**Disabled by default**: sme, critic, docs, designer — enable in config if needed.

**Authority rules** (in `.opencode/opencode-swarm.json`):
- coder: write-scoped to `src/Modules/**`, `clients/**`, `opencode/**`, `.agents/**`, `deploy/**`
- coder: DENY `src/BuildingBlocks/**`, `src/Host/**` (architect-only)

---

### B. Manual Multi-Session Mode (this protocol)

**No plugin required**. Single opencode session, state in `live/` + `board.md` + `00-Index.md`.
**Ritual is self-starting** — see `workflows/current/session-protocol.md` (canonical) and
`readme.md` "Self-starting" section. The human should never need to say "follow the ritual."

| Step | Command / Action | Artifact updated |
|---|---|---|
| Start session | Agent reads protocol + STATUS + live/ (self-starting) | `live/sess-<id>.md` |
| Heartbeat | `coordination.ps1 -Heartbeat` (every turn) | `live/sess-<id>.md` timestamp |
| Claim task | Agent writes to `live/board.md` | `board.md` |
| Build + test | `dotnet build` + `dotnet test --no-build` | terminal |
| Stage for review | `git add <explicit paths>` + show diff | staged diff |
| Wave close-out | `coordination.ps1 -CloseOut` + STATUS.md append | `STATUS.md`, `00_summary/` |
| Lessons | `coordination.ps1 -Lesson "..."` | `live/lessons.md` |

---

## 3. QA Gates & Orchestrator Integration

| Gate | Tool | When | Block |
|---|---|---|---|
| **Clean build** | `dotnet build` | After every code change | hard |
| **Tests pass** | `dotnet test --no-build` | After every code change | hard |
| **Blazor WASM verify** | `verify.ps1` | Before wave close-out | hard |
| **MAUI verify** | `verify-hybrid.ps1` | Before wave close-out (MAUI only) | hard |
| **bUnit tests** | `dotnet test` (Blazor projects) | After every Blazor change | hard |
| **Playwright probe** | `walkthrough/run-probes.mjs` | After UI changes | advisory |
| **SAST scan** | `pre_check_batch` / Semgrep | Phase boundaries | hard (new findings) |
| **Drift verify** | `critic_drift_verifier` | Phase completion | advisory |
| **Coordination gate** | `coordination.ps1 -Gate` | Before staging | hard |

### Decision flowchart

```
Code changed?
├─ YES → dotnet build → dotnet test
│         ├─ FAIL → fix → retry
│         └─ PASS → wave complete?
│                   ├─ YES → verify.ps1 → coordination.ps1 -CloseOut → stage for review
│                   └─ NO → next task
└─ NO → idle (heartbeat only)
```

---

## 4. File Inventory

| File | Purpose | Updated by |
|---|---|---|
| `STATUS.md` | Dashboard (current/next/obstacles) + append-only ledger | agent |
| `live/sess-<id>.md` | Per-session identity, heartbeat, focus, lessons | agent |
| `live/board.md` | Kanban: backlog → in-progress → done | agent |
| `live/lessons.md` | Failure-pattern registry + chronological log | agent |
| `live/locks/verify.lock` | Mutex for verify.ps1 execution | agent |
| `00-Index.md` | Roadmap: all phases + status | agent |
| `plan.md` | Phase plan with task status markers | agent |
| `implementation.md` | Wave-level implementation log | agent |
| `00_summary/implementation-summary-*.md` | Wave close-out evidence | agent |
| `readme.md` | Track reference (scope, dev servers, DoD) | human |
| `PROJECT.md` | Project facts and pointers | human |
