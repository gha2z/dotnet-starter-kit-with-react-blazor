# PROJECT — AddBlazorFrontends track

> Stable facts: what this track is, the apps, ports, layout. Process lives in
> `workflows/current/session-protocol.md`; current state in `STATUS.md`.

## Purpose

Reach and hold **zero functional + visual parity** between the frozen React reference pair
(`clients/admin` :5173, `clients/dashboard` :5174) and the Blazor WASM twins
(`clients/admin-blazor` :5175, `clients/dashboard-blazor` :5176), plus the MAUI Hybrid app
(`clients/FSH.Hybrid`). React = minimum parity, better = bonus. React files are FROZEN
(Golden Rule 11, AGENTS.md).

## Apps & ports

| App | Stack | Dev URL |
|---|---|---|
| React admin (reference, frozen) | React 19 + Vite 7 + shadcn | http://localhost:5173 |
| React dashboard (reference, frozen) | React 19 + Vite 7 + shadcn | http://localhost:5174 |
| Blazor admin | MudBlazor 9 WASM | http://localhost:5175 |
| Blazor dashboard | MudBlazor 9 WASM + SSE | http://localhost:5176 |
| API | .NET 10 minimal APIs | https://localhost:7030 |
| Aspire dashboard | orchestration + logs | https://localhost:15888 |

Whole stack: `dotnet run --project src/Host/FSH.Starter.AppHost` (Postgres, Redis, MinIO,
migrator, API, all front-ends). Seeded logins: `admin@acme.com` / `alice@acme.com` …
password `Password123!` (tenant `acme`); admin app: `superadmin@root.com` (tenant `root`).

## Layout

- `Phase-XX-<Mission>/` — one folder per mission: `spec.md` (human) + `plan.md` (agent,
  confirmed before build) + `implementation.md` (agent log with evidence).
- `live/` — multi-session coordination: `sess-<id>.md` (identity + heartbeat + current focus,
  kept short), `board.md` (cross-session rows only), `lessons.md` (failure-pattern ledger).
- `00_summary/` — point-in-time wave evidence (implementation-summary-*.md), referenced from
  mission implementation logs.
- `walkthrough/` — the Playwright driver, screen registries, probes, and visual evidence.
- `zones.md` — ownership authority parsed by `coordination.ps1`.
- `WORKFLOW-GUIDE.md` — task→skill map + the real-browser walkthrough gate (GR 11).
- `verify.ps1` / `verify-hybrid.ps1` — handoff verification (clean build → suites → audits).

## Key architectural facts (Blazor twins)

- Shared RCL `clients/BlazorShared`: auth (ITokenStore, AuthStateProvider, AuthDelegatingHandler),
  services (IChatService, IUserService, …), components, theming, realtime, runtime config.
- Runtime config from `/config.json` (not env): `apiBaseUrl`, `inactivityTimeoutMinutes`,
  `dashboardUrl`, …
- SignalR hub `/api/v1/realtime/hub` (chat + notifications); dashboard also uses SSE.
- MudBlazor 9 gotchas are documented canonically in `.agents/rules/frontend/blazor-shared.md`.
- Verification stack: bUnit suites + Playwright probes in `walkthrough/` + real-browser
  walkthroughs with vision inspection.

## Decisions log (stability-relevant)

- React pair is the reference; never modify it (except explicit user instruction).
- Parity bugs are fixed in the Blazor twins or BlazorShared; server fixes allowed with evidence.
- Probes live in `walkthrough/probe-*.mjs` and are committed alongside fixes.
