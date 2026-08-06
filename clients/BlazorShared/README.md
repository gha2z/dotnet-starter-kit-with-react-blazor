# FSH.BlazorShared — Shared RCL (both WASM apps)

Shared shell, UI components, auth plumbing, theming and real-time services used by
`FSH.Admin.Wasm` and `FSH.Dashboard.Wasm`. **Owned by the Main stream** — changes here have a wide
blast radius; see `.agents/rules/buildingblocks-protection.md`-style caution and the coordination
readme (`opencode/addBlazorFrontends/readme.md`) before editing.

## Areas

| Folder | Contents |
|---|---|
| `Auth/` | `ITokenStore` (+ WASM localStorage impl), `AuthenticationStateProvider`, delegating HTTP handler, `PermissionsProvider` |
| `Components/` | The `Fsh*` UI components below |
| `Formatting/` | Currency/date/bytes formatters shared by both apps |
| `Infrastructure/` | `RuntimeConfigService` (loads `config.json`), `ApiException`, CORS/API plumbing |
| `Models/` | Shared DTOs (`PagedResult<T>`, nav specs, user/session… ) |
| `Permissions/` | C# permission catalogs (mirror of server constants) |
| `Realtime/` | SignalR hub connection service |
| `Sse/` | Server-Sent Events service (dashboard) |
| `Theming/` | `FshThemeService` (light/dark/system + palette), theme persistence |
| `wwwroot/` | `css/fsh.css` (app shell styles), `js/fshError.js` |

## Component library

| Component | Purpose |
|---|---|
| `FshPageHeader` | Page title (h1) + optional meta/subtitle + action slot |
| `FshEmptyState` | Centered icon + title + description + optional action for empty lists |
| `FshErrorBand` | Inline error banner for failed loads (with retry) |
| `FshErrorBoundary` | ErrorBoundary wrapper around page content with reload button |
| `FshConfirmDialog` / `FshConfirmDialogContent` | Async confirmation dialog used by destructive actions |
| `FshFilterBar` | Row of filter controls + clear button (used by list pages) |
| `FshPager` | Pager controls (prev/next + page x of y) for server-paged lists |
| `FshPermissionGate` | Renders children only when the current user holds a permission |
| `FshKpiTile` / `FshStatTile` | Dashboard stat cards (large number + label + tone) |
| `FshToneIconTile` | Colored icon tile (used on Overview) |
| `FshStatusPill` | Small colored status badge |
| `FshMonogram` | Initials avatar tile (user menu, brand) |
| `FshNavSection` / `FshSectionRule` | Sidebar section rendering + dividers (nav specs from `Models`) |
| `FshLoadingRow` | Skeleton loading rows for lists |
| `FshNotificationBell` | Topbar bell with unread count + dropdown (SignalR-driven) |
| `FshNotFound` | 404 page content |

## Conventions

- New shared components: Razor + code-behind, `Fsh` prefix, no JavaScript interop unless unavoidable.
- Styling uses `fsh.css` classes + MudBlazor CSS variables (theme-driven).
- Both apps consume the same JWT flows; localStorage keys differ per app (`fsh.admin.*` / `fsh.dashboard.*`).
- See `.agents/rules/frontend/blazor-shared.md` for the full contract.
