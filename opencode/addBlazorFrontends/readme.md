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
2. Read `STATUS.md` — current state, next task
3. Read the latest `00_summary/implementation-summary-*.md` — what was last done
4. Read `.agents/rules/frontend/blazor-shared.md` — Blazor conventions (plus `blazor-admin.md`,
   `blazor-dashboard.md`, or `maui-hybrid.md` for the target app)
5. Load any relevant skills from `.agents/skills/` (e.g. `add-blazor-page`, `add-feature`,
   `setup-blazor-auth`, `setup-blazor-realtime`, `implement-blazor-list`, `implement-blazor-form`,
   `add-permission`) before you start
6. Verify builds + tests pass before starting work

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
- [ ] Fix any warning you introduce

---

## Build & Test Cadence

- After each unit of work: `dotnet build <target.csproj>` then `dotnet test <app>.Tests`
  (fast feedback, small output)
- Full solution build + both app suites at phase end (catches cross-project breakage)

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
