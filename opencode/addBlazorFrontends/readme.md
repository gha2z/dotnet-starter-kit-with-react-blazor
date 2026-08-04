# AddBlazorFrontends — Session Instructions

> **On session start:**
> 1. Read `00-Index.md` status line to understand current progress
> 2. Read the latest `00_summary/implementation-summary-*.md` for what was last done
> 3. Read the `plan.md` for the next phase to work on
> 4. Verify builds + tests pass (`dotnet build` + `dotnet test`) before starting work
> 5. After completing work, update docs and commit before stopping

---

Make sure you have updated the `./opencode/addBlazorFrontends/00-Index.md` and the `plan.md` for **only the phase(s) you worked on** in the current session. Other phase plans should be left untouched unless you also changed them.

Skip updating the hands-on-phase-x.md files until all phases have completed perfectly.

Write the **Last Update** date/time and introduce who you are right after the document main title, e.g:

```
# Blazor WASM + MAUI — Implementation Roadmap
Last Update: 2026-Aug-04 19:15:00, by: opencode (auto/coding, model: opencode/mimo-v2-pro-max).
```

```
# Phase 2 — Admin Feature Pages
Last Update: 2026-Aug-04 19:15:00, by: opencode (auto/coding, model: opencode/mimo-v2-pro-max).
```

This header goes in every root doc (`00-Index.md`, `00-Setup.md`, `99-Glossary.md`) and in every plan
document inside each phase folder — `plan.md`, or `pre-plan.md` in the `Phase 00-The foundation` folder
(it is the exception: literally named `Phase 00-The foundation`, with a space, and holds `pre-plan.md`
instead of `plan.md`).

### Model identity

Replace `<you>` with your actual identity, e.g. `by: opencode (auto/coding, model: <actual-model>).`

`auto`, `auto/coding` and other `auto/*` patterns are just OmniRoute routing combos — not the real LLM
(see https://github.com/diegosouzapw/OmniRoute/blob/release/v3.8.50/docs/getting-started/AUTO-COMBO-GUIDE.md).

**Primary method:** Use the model name from your system prompt (e.g., `opencode/mimo-v2-pro-max`).

**Fallback** (if your system prompt doesn't specify a model):
1. Log in to OmniRoute at http://localhost:20128/login (password: CHANGEME) to create a session cookie.
2. Fetch `GET http://localhost:20128/api/usage/call-logs?status=ok&limit=1` (authenticated by the browser session).
3. Read the `requestedModel` field from the JSON response — that is the real model name to write.

Example: `by: opencode (auto/coding, model: opencode/mimo-v2-pro-max).`

### Implementation summary

Write an MD file named `implementation-summary-<yyyy-MM-dd-HH-mm-ss>.md` (24-hour clock) in the
`./opencode/addBlazorFrontends/00_summary/` folder containing the summary of implementation steps:

```
# Implementation Summary <yyyy-MM-dd-HH-mm-ss>

---
**Description:** <The description summary>
**Creator:** <You>
**Duration:** <How long the implementation took place, ex: 30s, 1m 15s, 55m, 1h 12m 3s, 3h>
---

## <Phase N> - <Phase Name>: <Sub Feature N> - <Sub Feature Name>
---
- Step 1: <Description summary of the step 1>
- Step 2: <Description summary of the step 2>
...

## <Phase N> - <Phase Name>: <Sub Feature N> - <Sub Feature Name>
---
- Step 1: <Description summary of the step 1>
...
```

### Planning your next moves

Check `./opencode/addBlazorFrontends/00-Index.md`, the latest `implementation-summary-*.md` files in
`./opencode/addBlazorFrontends/00_summary/`, and the `plan.md` file in the relevant phase folder to plan your next moves.

Refer to the original repository (https://github.com/fullstackhero/dotnet-starter-kit),
website (https://fullstackhero.net/) to execute the plans, review your progress and keep building, testing, fixing,
and optimizing until all the plans completed and no more gaps between React 19 front-ends and Blazor Wasm front-ends
and .NET MAUI front-ends.

### Scope restriction

Do not write/modify anything in any folders other than blazor wasm and .net maui blazor hybrid front-ends project folders
and sub-folders (`./clients/FSH.Hybrid`, `./clients/admin-blazor`, `./clients/BlazorShared`, `./clients/dashboard-blazor`)
and `./opencode/addBlazorFrontends`.

Once you completed all phases, keep testing them to find the bugs and issues and fix and enhance them accordingly to ensure
these projects are production-grade ready. Feel free to add/modify phases, and plans when necessary.
You can modify the existing `AGENTS.md`, `README.md`, `README-template.md` along with Agent Skill.md and rules files,
and add the new ones related to the current BLAZOR WASM and .NET MAUI Hybrid front-ends projects when necessary.

Test the apps yourself and fix any issues relevant to the current development progress after you finish the
implementations. We don't want the end users seeing "An unhandled error has occurred" + "Reload" button shown up on any
single page/feature we have been implemented due to **the unfixed bugs**, so make sure to test thoroughly before
claiming completion.

---

### Dev servers

```
dotnet run --project src/Host/FSH.Starter.AppHost   # starts everything
```

| App | URL | Tenant | Email | Password |
|-----|-----|--------|-------|----------|
| admin (React) | http://localhost:5173 | root | admin@root.com | Password123! |
| dashboard (React) | http://localhost:5174 | acme | admin@acme.com | Password123! |
| admin-blazor | http://localhost:5175 | root | admin@root.com | Password123! |
| dashboard-blazor | http://localhost:5176 | acme | admin@acme.com | Password123! |
