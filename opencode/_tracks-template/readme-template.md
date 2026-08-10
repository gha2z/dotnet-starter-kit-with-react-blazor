# Track name

> **Track:** (this directory). Lifecycle owner for a work-stream: roadmap, status, live session
> state, and zones. The canonical in-repo example is `opencode/addBlazorFrontends/` — copy the
> file skeleton from `opencode/_tracks-template/` and rename.
>
> Requirements for the work this track ships live in `docs/spec/<NNN-name>.md`. Read the relevant
> rule files before working in any area (see `AGENTS.md`).

## This track

| Field | Value |
|---|---|
| **Track path** | `opencode/<track-name>/` |
| **App / stream** | (short description of what this track builds) |
| **Spec** | `docs/spec/NNN-<name>.md` |
| **Operated by** | {owner — who decides scope changes} |
| **Zones** | see `zones.md` |

## File map

| File | Purpose |
|---|---|
| `00-Index.md` | Roadmap: phases + feature index (the tracking list). |
| `STATUS.md` | Current state — done now next, gates, baselines. Read fresh every session. |
| `readme.md` | Protocol core (session ritual, scope restriction, model identity) — learn it once, reuse per track. |
| `coordination.ps1` | Heartbeat / lock / gate / zone checks (fails closed). |
| `verify.ps1`, `verify-hybrid.ps1` | Scripted verification gates non-negotiable. |
| `live/` | Session state: `board.md`, session files, `locks/`. |
| `zones.md` | Which code areas this track may touch; what it may never touch. |
| `Phase-NN-*/plan.md` | Per-phase: `FR-###` feature list mapped from the spec. |

## Operating rules (shared with every track)

1. **No plan, no code** — a phase plan must exist and be approved before coder work.
2. **Never touch another track's zone.** Coordination fails closed.
3. **Every feature independently verifiable** — `verify.ps1` (or the track-specific gate) before
   calling anything done.
4. **Stage by explicit path**, show `git diff --cached --stat`, wait for user approval. Never
   `git add -A`, never `git push`.
5. **Docs travel with the change** (Golden Rule 10).
6. **One session = one feature** (or one planning pass). Not one whole phase.

## Starting work

1. Read `00-Index.md` + `STATUS.md` + all of `live/` **fresh**.
2. Open the spec `docs/spec/NNN-<name>.md`, confirm the next `FR-###` set is bounded.
3. Plan → get approval → coder → reviewer/test_engineer → verify → stage for approval.