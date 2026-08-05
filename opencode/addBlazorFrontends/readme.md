# AddBlazorFrontends — Session Instructions

## Commit Policy

**NEVER commit until the user explicitly approves.**

After completing a feature:
1. `git add -A`
2. Show: `git diff --cached --stat` + one-line summary of what changed
3. Wait for user approval
4. Then commit

---

## Session Start Ritual

On session start:
1. `git status` — confirm the tree is clean or matches the expected in-progress work
2. Determine your actual model identity (see "Model Identity Convention" below) and record it
   for today's docs
3. Read `./opencode/addBlazorFrontends/STATUS.md` — current state, next task
4. Read the latest `./opencode/addBlazorFrontends/00_summary/implementation-summary-*.md` — what was last done
5. Read `.agents/rules/frontend/blazor-shared.md` — Blazor conventions (plus `blazor-admin.md`,
   `blazor-dashboard.md`, or `maui-hybrid.md` for the target app)
6. Load any relevant skills from `.agents/skills/` (e.g. `add-blazor-page`, `add-feature`,
   `setup-blazor-auth`, `setup-blazor-realtime`, `implement-blazor-list`, `implement-blazor-form`,
   `add-permission`) before you start
7. Verify builds + tests pass before starting work

---

## Model Identity Convention

**Never guess or copy a model identity from memory or older docs.** Identity drift happened before —
a session "fixed" the docs to a wrong model name without verification, and it propagated for two
sessions. Determine it fresh every session.

**`auto/*` is a combo name, NOT a model.** A combo routes through a gateway (e.g. `omniroute`) to an
actual model (e.g. `big-pickle`). Writing the combo as the model is wrong.

### How to determine your actual model identity

1. **Inspect the opencode raw-message JSON** (system prompt / context window / raw messages) for the
   `modelID` + `providerID` of your current session.
2. **If `modelID` is a concrete model** (not `auto`/`auto/*`, e.g. `mimo-v2.5-free` via provider
   `opencode`) → use it directly. **No gateway log query needed.**
3. **If `modelID` is an `auto` combo** (e.g. `auto/coding`, provider `omniroute`) → resolve the real
   routed model through the omniroute log:
   - Open `http://localhost:20128/login`, enter the password (see dev machine secrets)
   - Open `http://localhost:20128/api/usage/call-logs?status=ok&limit=1`
   - Read the `model` field of the most recent `ok` entry (e.g. `big-pickle`)

### Identity format in docs

Use the resolved model everywhere identity appears:

- Headers: `Last Update: <yyyy-MM-dd HH:mm:ss>, by: opencode (auto/coding, model: <routed-model>).`
- Summaries: `**Creator:** opencode (auto/coding, model: <routed-model>)`

Use the actual session timestamp (24-hour clock), not a value copied from a prior file.

---

## Pre-Flight Convention

Before writing any `.razor` file, read an existing working page in the same project to confirm API conventions (parameter names, component availability, attribute syntax). This prevents iterative build-error-fix cycles.

---

## Definition of Done (every feature)

- [ ] Service method + DI registration exist — no invented DTOs/endpoints
- [ ] bUnit test asserts the **specific behavior** (gated content, redirect, data shown), not just
      "renders without error"
- [ ] Target project builds 0 warnings; that app's suite is green
- [ ] If behavior is not bUnit-testable (menu opens on click, real navigation), list the exact
      manual-verification steps and flag them in the summary
- [ ] Docs written this session use the **freshly-verified** model identity (see Model Identity
      Convention) + actual session timestamp — never a value copied from prior files
- [ ] Fix any warning you introduce

---

## Build & Test Cadence

- After each unit of work: `dotnet build <target.csproj>` then `dotnet test <app>.Tests`
  (fast feedback, small output)
- **Never trust an incremental build for handoff.** The Razor source generator caches in `obj/` —
  `--no-incremental` alone does NOT flush it, so a stale `obj/` can report "0 errors" for code that
  fails to compile (real incident: `Icons.Material.Filled.Bell` shipped "green"). Before any handoff:
  1. Delete `obj/` + `bin/` for every changed project (and its `.Tests`)
  2. `dotnet build <target.csproj>` — must be 0 warnings, 0 errors
  3. `dotnet test <app>.Tests` — suite must be green
  4. Run `dotnet build` on the sibling Blazor app too (BlazorShared is shared — cross-app breakage)
- Full solution build + both app suites at phase end (catches cross-project breakage)
- **Icon smoke check (MudBlazor):** before handoff, grep all `Icons.Material.*` references in the
  pages you touched and verify each constant exists in the referenced MudBlazor assembly
  (`Icons.Material.Filled.*` etc.). Icon names drift between MudBlazor versions (`Bell` → use
  `Notifications`).

---

## Scope Restriction

Modify ONLY:

- `./clients/BlazorShared` · `./clients/admin-blazor` · `./clients/dashboard-blazor` · `./clients/FSH.Hybrid`
- `./opencode/addBlazorFrontends`
- `./.agents/rules/frontend/{blazor-shared,blazor-admin,blazor-dashboard,maui-hybrid}.md` — OUR docs
- `./.agents/skills/{add-blazor-page,add-maui-hybrid-feature,add-permission-csharp,implement-blazor-form,implement-blazor-list,setup-blazor-auth,setup-blazor-realtime,setup-blazor-sse}/SKILL.md` — OUR skills

NEVER modify (upstream baseline / React reference):

- `clients/admin` · `clients/dashboard` (React apps — READ-ONLY parity source)
- `AGENTS.md` · `CLAUDE.md` · `GEMINI.md` · `.github/**` · `deploy/**` · `src/**`
- `.agents/rules/**` (all other rule files) · `.agents/skills/*` (all other skills) · `.agents/workflows/**`

---

## Audit / Review Convention

For parity/review tasks spanning many files, delegate to an explore subagent and require it to write
full findings to a temp file while returning only a compact severity-ranked list
(Critical/High/Medium/Low) — keeps the working context small.

---

## Implementation Summary

Write an MD file named `implementation-summary-<yyyy-MM-dd-HH-mm-ss>.md` (24-hour clock) in
`./opencode/addBlazorFrontends/00_summary/` containing the summary of implementation steps:

```
# Implementation Summary <yyyy-MM-dd-HH-mm-ss>

---
**Description:** <The description summary>
**Creator:** <You>
**Duration:** <How long the implementation took place>
---

## <Phase N> - <Phase Name>: <Sub Feature N> - <Sub Feature Name>
---
- Step 1: <Description summary of the step 1>
...
```

---

## Hands-On Files

Skip the `hands-on-phase-*.md` files until all phases have completed perfectly. After each phase
completes, write a brief `phase-N-complete.md` snapshot (what was built, known limitations, files
created/modified) in the phase folder. Write the full hands-on files from those snapshots + implementation
summaries after all phases are done.

---

## Dev Servers

```
dotnet run --project src/Host/FSH.Starter.AppHost   # starts everything
```

| App | URL | Tenant | Email | Password |
|-----|-----|--------|-------|----------|
| admin (React) | http://localhost:5173 | root | admin@root.com | Password123! |
| dashboard (React) | http://localhost:5174 | acme | admin@acme.com | Password123! |
| admin-blazor | http://localhost:5175 | root | admin@root.com | Password123! |
| dashboard-blazor | http://localhost:5176 | acme | admin@acme.com | Password123! |
