# FSH.Admin.Wasm — Blazor WASM Operator Console

The platform-operator app, twin of the React app in `clients/admin` (port 5173). Built with MudBlazor 9
on .NET 10 WASM, targeting the FullStackHero API.

| | |
|---|---|
| Dev URL | http://localhost:5175 |
| API base | `https://localhost:7030` (from `wwwroot/config.json`, runtime-loaded — not build-time) |
| Auth | JWT in localStorage (`fsh.admin.accessToken` / `refreshToken` / `tenant` / `permissions`) |
| Realtime | SignalR hub (notifications, chat) |
| Tests | bUnit `FSH.Admin.Wasm.Tests` (158) · Playwright E2E `FSH.Admin.Wasm.E2E.Tests` (9) |

## Run

```bash
dotnet run --project clients/admin-blazor/FSH.Admin.Wasm   # → http://localhost:5175
```

The API must be running (`dotnet run --project src/Host/FSH.Starter.Api`). `config.json` is loaded at
runtime, so the API URL can be changed without rebuilding.

## Testing

```bash
dotnet test clients/admin-blazor/FSH.Admin.Wasm.Tests           # bUnit (no dependencies)
dotnet test clients/admin-blazor/FSH.Admin.Wasm.E2E.Tests       # Playwright — needs the dev
                                                                 # server on 5175 (auto-started;
                                                                 # reuses one already answering)
```

The E2E suite is fully route-mocked (no backend) — same harness pattern as the dashboard suite.

## Architecture & conventions

- Pages follow the React-admin parity contract (`clients/admin/src/pages`) — see
  `.agents/rules/frontend/blazor-admin.md` and `blazor-shared.md`.
- Shared shell/UI components live in `clients/BlazorShared` (see its README) — the Main stream owns it.
- Every endpoint-gated action has a mirrored permission: server constant + C# permission catalog
  + `FshPermissionGate` + route guard. Adding a permission touches all four (see
  `.agents/skills/add-permission-csharp`).
- Cross-app flows: impersonation links to the dashboard, the dashboard URL comes from `config.json`.

## Release publish

- Trimming: `BlazorWebAssemblyEnableTrimming=true`; AOT deliberately off (see the dashboard README /
  Phase-06 plan for the benchmark).
- Publish output is static-hostable (`dotnet publish` → serve `wwwroot`). Do NOT re-add
  `OverrideHtmlAssetPlaceholders` — it breaks static hosting.
