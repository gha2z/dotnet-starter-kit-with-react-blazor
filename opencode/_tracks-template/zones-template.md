# <track-name> — zones

> Which files this track may touch, and what it may never touch. Coordination fails closed:
> straying out of zone is an announcement + re-route through `live/board.md`, not a silent fix.

## May touch

- … (e.g. `clients/dashboard-blazor/**`, `clients/BlazorShared/**` for cross-app)
- `opencode/<track-name>/**` (per-file)
- `docs/spec/NNN-<name>.md` (spec updates with owner)

## Never touch without approval

- `src/BuildingBlocks/**` — protected (see `AGENTS.md` Golden Rule 4)
- … other tracks' zones
- `clients/admin` + `clients/dashboard` — parity reference (read-only)
- `AGENTS.md`, `CLAUDE.md`, `GEMINI.md`, `.github/**`, `deploy/**`, root `.opencode/`

## Shared (announce on `live/board.md` before editing)

- `.agents/rules/**`, root `README.md`, `HUMAN-GUIDE.md`