# FSH.Dashboard.Wasm — Blazor WASM Tenant Dashboard

The tenant-facing operator app, twin of the React app in `clients/dashboard` (port 5174). Built with
MudBlazor 9 on .NET 10 WASM, targeting the FullStackHero API.

| | |
|---|---|
| Dev URL | http://localhost:5176 |
| API base | `https://localhost:7030` (from `wwwroot/config.json`, runtime-loaded — not build-time) |
| Auth | JWT in localStorage (`fsh.dashboard.accessToken` / `refreshToken` / `tenant` / `permissions`) |
| Realtime | SSE (`/api/v1/sse/*`) + SignalR hub |
| Tests | bUnit `FSH.Dashboard.Wasm.Tests` (179) · Playwright E2E `FSH.Dashboard.Wasm.E2E.Tests` (17) |

## Run

```bash
dotnet run --project clients/dashboard-blazor/FSH.Dashboard.Wasm   # → http://localhost:5176
```

The API must be running (`dotnet run --project src/Host/FSH.Starter.Api`). `config.json` is loaded at
runtime, so the API URL can be changed without rebuilding.

## Testing

```bash
dotnet test clients/dashboard-blazor/FSH.Dashboard.Wasm.Tests           # bUnit (no dependencies)
dotnet test clients/dashboard-blazor/FSH.Dashboard.Wasm.E2E.Tests       # Playwright — needs the dev
                                                                        # server on 5176 (auto-started;
                                                                        # reuses one already answering)
```

The E2E suite is fully route-mocked (no backend): it seeds an authenticated session via localStorage
init scripts and answers every API call with mocked JSON + CORS headers. Shell mocks cover SSE/SignalR.
Key gotchas are documented in `FSH.Dashboard.Wasm.E2E.Tests/Infrastructure/E2EHelpers.cs`.

## Architecture & conventions

- Pages follow the React-dashboard parity contract (`clients/dashboard/src/pages`) — see
  `.agents/rules/frontend/blazor-dashboard.md` and `blazor-shared.md`.
- Shared shell/UI components live in `clients/BlazorShared` (see its README) — Main stream owns it.
- Server-side paging for all high-volume lists; debounced search (250 ms + CTS) on server-searched
  fields — see `Pages/Identity/UsersListPage.razor.cs` (canonical pattern).
- Permission-gated UI uses `FshPermissionGate`; the dashboard hides (never 403s) — React parity.
- Impersonation: entering ends at `/impersonation/ended`; the tenant header + `x-fsh-app: dashboard`
  flow matches the React app.

## Release publish

- Trimming: `BlazorWebAssemblyEnableTrimming=true` (verified via static-hosted publish smoke).
- AOT: deliberately OFF — benchmarked 2.5x size + 2x boot for no CPU-heavy gain (see
  `opencode/addBlazorFrontends/Phase-06-Polish-And-Perf/plan.md`).
- Publish output is static-hostable (`dotnet publish` → serve `wwwroot`). Do NOT re-add
  `OverrideHtmlAssetPlaceholders` — it breaks static hosting.
