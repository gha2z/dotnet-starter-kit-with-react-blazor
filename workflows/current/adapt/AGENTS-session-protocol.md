# AGENTS.md section — Session protocol (paste into AGENTS.md as-is)

The exact text below is installed in this repo's `AGENTS.md`. It is the **self-start guarantee**:
any coding agent that reads AGENTS.md starts the ritual itself — no human reminder, no
tool-specific knowledge required. The full protocol lives in `workflows/current/session-protocol.md`.

---

## Agent session protocol (MANDATORY, self-starting)

Every coding agent, any session, including a session resumed after context compaction, MUST run
this on every task. Progress lives **on disk** — never reconstruct state from memory.

1. **Read fresh** (whole files, in order): this file → `workflows/current/session-protocol.md` →
   track's `STATUS.md` → all track `live/*.md` → latest track `00_summary/implementation-summary-*.md`.
2. **Identify** — session id (user-named, e.g. `sess-main`), model identity **verified fresh**
   (never copied from older docs; `(unresolved)` if unverifiable), scope, heartbeat — in
   `live/sess-<id>.md` from `live/_template.md`; run `coordination.ps1 -Session <sid> -Start`.
3. **Lessons before acting** — `coordination.ps1 -Session <sid> -Lessons` (failure-pattern
   registry; a matching event is appended with `[RECUR]` and its registered remedy).
4. **Overlap check** — claimed elsewhere / another session's zone (`zones.md`) or touched files?
   Stop, post on `live/board.md`, take another task.
5. **Every user turn** — heartbeat (`-Heartbeat`), and at turn end append exactly one line:
   `coordination.ps1 -Session <sid> -Lesson "<failure -> root cause -> remedy -> proof>"`.
   If nothing failed, write one improvement or verified pattern. No empty turns.
6. **Wave close-out** — summary to `00_summary/` per `_template.md` (with `## Lessons` section) →
   one append-only line to `STATUS.md` → `coordination.ps1 -Session <sid> -CloseOut` (fail-closed)
   → stage by **explicit paths** (`git add <paths>`, never `-A`), show `git diff --cached --stat`,
   wait for approval. Never `git push`.
7. **Verify** — the track's verify script green (clean build 0 warnings + suites) before staging;
   numbers from real runs this session only.
8. **Continuity** — end each session with a `Next: <task id>` line in your session file and the
   STATUS ledger current, so any future session resumes in one read.
9. **Memory rule** — model memory tools (`ctx_memory`, `ctx_search`, …) are recall aids and
   scratch; the disk is the source of truth. Disk disagrees → disk wins.

Track location for this repo: `opencode/addBlazorFrontends/` (its `readme.md` + `coordination.ps1`
+ `verify.ps1` are authoritative for that track).