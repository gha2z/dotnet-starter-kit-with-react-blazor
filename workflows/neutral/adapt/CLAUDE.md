# Claude Code bridge — Enhanced workflow (neutral)

This repo runs the enhanced agentic workflow. The canonical protocol is
`workflow/neutral/session-protocol.md` (resolve the `workflow/` prefix to wherever you placed the
package). **Read that file first — it is mandatory and self-starting**: on every task, run the
Session Start Ritual (§1 of the protocol) without being asked.

Quick pointers:

- Ritual + coordination + close-out: `workflow/neutral/session-protocol.md`
- Progress: work-stream `STATUS.md` (append-only ledger) + `live/*.md` + `00_summary/`
- Mechanical gate (heartbeat/ownership/lessons/locks): `pwsh workflow/neutral/coordination.ps1 -Session <sid> …`
- Skills: `.agents/skills/*/SKILL.md` (plain markdown — read before the task)
- Rules: `.agents/rules/**` (read the file for your area)

State lives on disk, never in conversation memory. If this file and a session's memory disagree,
the disk wins.