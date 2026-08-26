# workflows/current — Enhanced workflow · current environment

Self-contained copy of the enhanced agentic workflow for **this repo, this environment**:
opencode + opencode-swarm + dotnet-skills plugin suite, operating the Blazor WASM twins, MAUI
Hybrid, and the frozen React reference pair.

## Contents

| File | What |
|---|---|
| `session-protocol.md` | **Canonical protocol core** — ritual, coordination, close-out, self-improvement, continuity (`session-protocol.md:1`) |
| `coordination.ps1` | Mechanical gate **v2** — same CLI as the live script, but the zone→owner map is parsed from the track's `zones.md` (single authority; no script/doc drift) |
| `task-skills.md` | Canonical task→skill map — FSH recipes + dotnet plugin suite, with the FSH-wins conflict rule |
| `verify.md` | Verification contract + gate table + cadence + lock etiquette |
| `adapt/AGENTS-session-protocol.md` | The exact section installed in `AGENTS.md` (self-start guarantee) |

## Wire-up (already done in this repo)

- `AGENTS.md` carries the ritual section (from `adapt/`) → any agent self-starts.
- Live track stays where it is: `opencode/addBlazorFrontends/` — **do not edit it here**; this
  package documents it. Its `readme.md`, `coordination.ps1` (v1, with the lesson-ledger additions)
  and `verify.ps1` remain authoritative for that track's day-to-day; adopt this v2 script during
  the next workflow-consolidation wave.
- `opencode/AGENTIC-GUIDE.md`, `workflows/_tracks-template/`, `docs/spec/` remain the repo guide,
  template scaffold, and requirements home.
- New-workstream seeding: `opencode/addBlazorFrontends` itself was seeded from
  `workflows/_tracks-template/` (see `AGENTIC-GUIDE.md` §4).

## If you copy this package to a different opencode repo

1. Copy the folder to `<repo>/workflows/current/`.
2. Paste `adapt/AGENTS-session-protocol.md` into that repo's `AGENTS.md` and fix the two path
   lines at the end (track location + `workflows/current/session-protocol.md`).
3. Seed a track from that repo's own template (or this repo's `workflows/_tracks-template/`):
   copy to `opencode/<track>/`, set the owner field in `zones.md`, add the first session.
4. Create the entries in that repo's `AGENTS.md` "Rules index" pointing at
   `workflows/current/*` — canonical docs for the new home.