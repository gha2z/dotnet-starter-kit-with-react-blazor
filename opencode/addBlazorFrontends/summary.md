# Summary

## Objective
- Fix runtime failures on the Blazor dashboard Overview page (localhost:5176) reported by the user against the running Aspire stack: `TenantStatusDto` + `AuditEventType` deserialization errors and SSE 401 loop — then close remaining React ↔ Blazor gaps.

## Important Details
- User-visible errors (verbatim): `Failed to load tenant status: DeserializeUnableToConvertValue, FSH.BlazorShared.Models.Dashboard.TenantStatusDto Path: $.id | LineNumber: 0 | BytePositionInLine: 12.` and `Failed to load recent audits: DeserializeUnableToConvertValue, FSH.BlazorShared.Models.Audits.AuditEventType Path: $.items[0].eventType | LineNumber: 0 | BytePositionInLine: 127.`
- Console showed `POST https://localhost:7030/api/v1/sse/token` → 401, then refresh 401. React cleans tokens + redirects on expiry (`clients/dashboard/src/lib/api-client.ts`); Blazor mirrors this via dashboard `App.razor.cs` `TokensChanged → OnTokensChanged` handler — so SSE 401 recovery is session-expiry handling, not an SSE bug.
- Server `TenantStatusDto.Id` is a **string** tenant key (e.g. "root"), not Guid — React mirrors string too (`clients/dashboard/src/api/billing.ts:75`).
- API globally serializes single-value enums as **string names** (`JsonStringEnumConverter` at `src/Host/FSH.Starter.Api/Program.cs:23`; `[Flags]` enums opt back to numeric). React mirrors every audit enum as a string union (`clients/dashboard/src/api/audits.ts`).
- Audits handler always orders by `OccurredAtUtc` desc and ignores `Sort`; pagination params are `PageNumber`/`PageSize` (`GetAuditsQuery.cs`, `GetAuditsQueryHandler.cs`).
- Dashboard `App.razor.cs` owns the SSE connection lifecycle (start/stop on login/logout) — pages should only observe `SseService.ConnectionChanged`, never call `StartAsync` themselves.
- SSE endpoints: `POST /api/v1/sse/token` (JWT) issues opaque short-lived token; `GET /api/v1/sse/stream?token=` streams (`src/BuildingBlocks/Web/Sse/SseEndpoints.cs`).
- MudBlazor 9.7 API drift (all fixed): `MudProgressCircular` (not `MudCircularProgress`), `MudChip T="string"`, `MudRadioGroup @bind-Value` (no `SelectedOption`), `MudRadio Value=` (no `Option=`), `MudTextField` has no `Size=`/`MinLength=`, `MudIconButton` uses `aria-label=` (not `AriaLabel=`), no `MudListItemAvatar`/`MudListItemText`/`Clickable=`.
- `FlexibleEnumJsonConverter<T>` accepts string names **or** integer forms (defensive against legacy numeric payloads).
- BlazorShared builds clean after removing unused `config` ctor param from `DashboardService`; duplicate `AddScoped<IAuthService, AuthService>()` in admin `Program.cs` removed; orphaned `clients/BlazorSharedComponents/` folder (5 stray `.razor` leftovers, unreferenced by any project) deleted.
- Docs synced: `00-Index.md` (Phase 3.1 Overview ✅, dashboard 24/24), Phase-02 (147/147, 0 warnings + MudBlazor cleanup note), Phase-03 (3.1 done section, corrected SSE architecture note — pure C# `HttpClient` streaming, NOT JS interop), Phase-07 (status, gap table + build order).

## Work State
### Completed
- `DashboardDtos.cs`: `TenantStatusDto.Id` changed `Guid` → `string` (matches server + React).
- Created `clients/BlazorShared/Models/Json/FlexibleEnumJsonConverter.cs`; applied `[JsonConverter(typeof(FlexibleEnumJsonConverter<AuditEventType>))]` and `...<AuditSeverity>` in `AuditDtos.cs`.
- `DashboardService.GetRecentAuditsAsync` now queries `?PageNumber=1&PageSize={take}` instead of ignored `take`/`orderBy`.
- OverviewPage: removed `SetupSseConnectionAsync` + `_sseSubscription`; subscribes `SseService.ConnectionChanged += OnSseConnectionChanged` (marshals `InvokeAsync` → `_isSseConnected = SseService.IsConnected`); `Dispose()` unsubscribes.
- Verified before the SSE rework: BlazorShared/admin/dashboard all build **0 warnings / 0 errors**; admin tests **147/147**; dashboard tests **24/24** (6 new `OverviewPageTests`).

### Active
- OverviewPage.razor markup still had `@if (_loadingSse) { MudProgressCircular } else if (_isSseConnected) { LIVE chip }` around RECENT ACTIVITY header — `_loadingSse`/`_sseError` are now dead (SSE start moved to App root) and need removal.
- The SSE rework had not been rebuilt/retested yet (dashboard suite + build pending after the `_loadingSse` cleanup).

### Blocked
- (none)

## Next Move
1. In `OverviewPage.razor` RECENT ACTIVITY header: drop the `_loadingSse` `MudProgressCircular` branch; show LIVE chip only when `_isSseConnected`.
2. Remove unused `_loadingSse` and `_sseError` fields from `OverviewPage.razor.cs`.
3. Rebuild dashboard + BlazorShared (expect 0 warnings/0 errors) and rerun `dotnet test clients/dashboard-blazor/FSH.Dashboard.Wasm.Tests` (expect 24/24).
4. Ask user to hard-refresh http://localhost:5176/ and confirm: no FAILURE band, tenant status + recent activity render, LIVE chip reflects SSE state.
5. Sync Phase-03 plan (SSE ownership note: App root owns start/stop), then continue with 3.2 Activity per build order in `Phase-07-Parity-Completion/plan.md`.

## Relevant Files
- `clients/BlazorShared/Models/Dashboard/DashboardDtos.cs` — `Id` string fix.
- `clients/BlazorShared/Models/Json/FlexibleEnumJsonConverter.cs`, `clients/BlazorShared/Models/Audits/AuditDtos.cs` — enum wire-shape fix (string names + integer fallback).
- `clients/BlazorShared/Services/DashboardService.cs` — audit paging params.
- `clients/dashboard-blazor/FSH.Dashboard.Wasm/Pages/Overview/OverviewPage.razor` + `.razor.cs` — SSE observe-only rework; `_loadingSse`/`_sseError` cleanup pending.
- `clients/dashboard-blazor/FSH.Dashboard.Wasm/App.razor.cs` — SSE connection owner + `TokensChanged` session-expiry handler.
- `clients/dashboard-blazor/FSH.Dashboard.Wasm.Tests/Pages/Overview/OverviewPageTests.cs` — 6 tests (fake SSE service, `IsConnected => true`).
- `src/Host/FSH.Starter.Api/Program.cs`, `src/Modules/Multitenancy/Modules.Multitenancy.Contracts/Dtos/TenantStatusDto.cs`, `src/Modules/Auditing/.../GetAudits/GetAuditsQueryHandler.cs`, `src/BuildingBlocks/Web/Sse/SseEndpoints.cs` — server shapes driving the fixes.
- `clients/dashboard/src/api/billing.ts`, `clients/dashboard/src/api/audits.ts` — React parity references (string id, string-union enums).