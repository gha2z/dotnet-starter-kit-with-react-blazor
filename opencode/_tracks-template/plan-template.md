# <track-name> — Phase N plan (template)

> One plan per phase. Mapped from `docs/spec/NNN-<name>.md` — the FR-### ids must trace 1:1 to the
> spec's Musts. No code starts until this plan is approved.

**Phase:** N · **Title:** … · **Track:** `opencode/<track-name>/`
**Status:** draft | approved | in-progress | complete
**Approved by:** {user} on {date}

## Scope

- In: …
- Out: … (explicitly)

## Features in this phase

| FR | Acceptance (from spec) | Verification |
|---|---|---|
| `FR-001` | Must: … Given… when… then… | gate/command + expected |
| `FR-002` | … | … |

## Dependencies / risks

- …

## Definition of Done

- Every FR in this phase lands with its verification green.
- 0 build warnings; tests pass; docs/changelog updated for user-facing bits.
- Staged by explicit path; commit awaits user approval.