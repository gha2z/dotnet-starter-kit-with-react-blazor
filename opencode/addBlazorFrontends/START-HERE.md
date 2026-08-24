# START-HERE — read this first (human or agent)

**Resuming work? Three reads, always current:**

1. **[`STATUS.md`](./STATUS.md)** — top section is the living dashboard: current mission, next
   task, obstacles, priorities. History is below it (append-only).
2. **The current mission folder** (named in the dashboard, e.g. `Phase-09-UI-Parity-Hardening/`):
   - `spec.md` — the requirements (human-authored)
   - `plan.md` — the phased plan with ☐/🔄/✅ status markers (agent-authored; user confirms
     before build)
   - `implementation.md` — what was done per wave, with evidence and lessons (agent-authored)
3. **Session ritual** (agents): [`workflows/current/session-protocol.md`](../../workflows/current/session-protocol.md)
   — identify in `live/sess-<id>.md`, run `coordination.ps1 -Session <sid> -Start`, heartbeat +
   one lesson line per turn, close-out before staging. Stage by explicit paths, never `-A`,
   never push.

**Pointers:** repo-wide agent guide: [`AGENTS.md`](../../AGENTS.md) · app/layout facts:
[`PROJECT.md`](./PROJECT.md) · task→skill map: [`WORKFLOW-GUIDE.md`](./WORKFLOW-GUIDE.md) ·
ownership: [`zones.md`](./zones.md) · raw wave evidence: `00_summary/` · probes:
`walkthrough/`.

**Convention (per mission folder):** the human writes `spec.md`; the agent writes `plan.md`
(confirmed by the human before build) and keeps `implementation.md` current. Prove root causes
before fixing; a product bug fix gets a regression test where feasible; test counts live in
implementation.md/summaries (point-in-time evidence), not in living docs.
