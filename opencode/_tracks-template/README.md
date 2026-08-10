# _tracks-template — start a new track from here

> A track is a per-work-stream harness: roadmap, status, live session state, zones — the same
> protocol core that powers `opencode/addBlazorFrontends/`, factored so you don't copy/paste an
> entire working directory.

## When you need a new track

**Use case:** a distinct work-stream with its own owner, zone, and roadmap that runs (possibly in
parallel) alongside other tracks. Examples for this repo's future platform: `erp` (client
back-office), `pos` (standalone store management), `consumer` (retail mobile app). The parity work
stays in `addBlazorFrontends` — do not create a new track for it.

**Counter-case (don't):** if the work is a few features inside an existing track's zone, it's a
phase in that track's `00-Index.md`, not a new track. New tracks are expensive — zones must be
disjoint, verification gates must exist, an owner must be named.

## How to create one

1. **Copy the protocol core** from a working track:
   `copy-item opencode/addBlazorFrontends/<readme.md, coordination.ps1, verify*.ps1, board.md> opencode/<name>/`
   — or, if a bare skeleton is enough, copy the templates in this folder.
2. **Rename** `*-template.md` files to their real names (`00-Index.md`, `STATUS.md`, `plan.md`,
   `zones.md`, `readme.md`) and fill the `{…}` fields.
3. **Write the spec** — `docs/spec/NNN-<name>.md` (see `docs/spec/README.md`). The track's
   requirements live here, not in the track folder.
4. **Declare zones** in `zones.md`; assert they're disjoint from every other track before any
   parallel session is allowed.
5. **Point the coordinator** — ensure `coordination.ps1` exists in the new track (or supplant it
   with the shared one per the track's readme) and the live/ skeleton is present.
6. **Tell the world** — add a row to the platform track table in `HUMAN-GUIDE.md` and
   `opencode/AGENTIC-GUIDE.md`.

## Rule of thumb

> New track = new owner + disjoint zone + (spec | index | status | live). If any of those isn't
> real, it's a phase, not a track.