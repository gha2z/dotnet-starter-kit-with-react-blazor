# Verification Contract — Current Environment

> The non-negotiable gates. Numbers in any doc/summary come from a real run **this session** —
> never from memory. Baselines are live in the track's `STATUS.md`; refresh there, not in this file.
> Inside a shared checkout, take the verify lock first (`coordination.ps1 -LockVerify` /
> `-UnlockVerify`); coordination refuses builds while the lock exists.

## Gates

| # | Gate | Command | Failure = |
|---|---|---|---|
| 1 | Build, 0 warnings (errors are build failures) | `dotnet build <target.csproj>` / `dotnet build src/FSH.Starter.slnx` | rework before handoff |
| 2 | Unit tests | `dotnet test <app>.Tests` / full solution at phase end | rework |
| 3 | Handoff verification | track `verify.ps1` — deletes `obj/`+`bin/` (Razor source-gen cache!), builds both WASM apps counting warnings/errors, runs both bUnit suites, MudBlazor icon audit, prints a paste-ready verification block | blocks commit |
| 4 | MAUI Hybrid | track `verify-hybrid.ps1` — 4 TFMs build, 0 warnings | blocks commit |
| 5 | Architecture tests | `dotnet test src/Tests/Architecture.Tests` | blocks commit |
| 6 | Real-browser walkthrough | Playwright driver in the track `walkthrough/` — real clicks/navigation, console + network + visual checks. **bUnit is necessary but not sufficient** (AGENTS.md Golden Rule 11) | blocks commit for any Blazor/Hybrid page change |
| 7 | Backend integration | `dotnet test src/Tests/Integration.Tests` (requires Docker) | blocks phase |
| 8 | Frontend type check | `npx tsc --noEmit` + `npm run build` per React app (they are FROZEN — read-only reference; only running them) | informational |
| 9 | Lint | `dotnet format --verify-no-changes` | blocks commit |

## Cadence

- After each unit of work: build the target project, then its tests — fast feedback, small output.
- Full solution build + both app suites at phase end (catches cross-project breakage).
- `verify.ps1` variants: `-SkipClean`, `-SkipTests`. Run it inside a worktree when one is used;
  the main checkout cannot verify worktree state.
- Icon spot-check (MudBlazor): names drift between versions (`Bell` → `Notifications`).

## Verify lock etiquette

Create `live/locks/verify.lock` (atomic) **before** any clean/build/test which clears `obj/`/`bin/`
from the shared checkout; delete it after. While it exists, no other session builds or verifies.
A stale lock (creator's heartbeat > 12h) may be removed after a `board.md` announcement.
On Windows: VS open on the solution, `.vs` locks, or running dev servers can abort the clean step —
stop them or use `-SkipClean`.