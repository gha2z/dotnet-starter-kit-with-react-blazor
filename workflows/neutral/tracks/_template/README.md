# <track> — Session Instructions (template)

> Copy this folder to `<repo>/workflow/<track>/` to start a work-stream. Replace every
> `<placeholder>` below. This readme is the track's contract; the canonical protocol is
> `workflow/neutral/session-protocol.md`.

## Commit Policy

- **Never commit until the user explicitly approves.**
- Stage only your session's files by explicit path — `git add <paths>`. NEVER `git add -A` in a
  shared checkout (it sweeps other sessions' uncommitted work into your commit).
- Show `git diff --cached --stat` + a one-line summary; wait for approval; then commit.
- **Never `git push`** — pushes are user-managed.
- Run the gate first: `pwsh workflow/neutral/coordination.ps1 -Session <sid> -Gate`
  (exit code 1 = block staging).

## Session Start Ritual (self-starting — never wait for a reminder)

1. `git status` — clean or expected in-progress work.
2. Session id = the user's name for this session (`sess-main`, …). Ask if unnamed.
3. Model identity — **verify fresh this session**; never copy from older docs (`(unresolved)` if
   unverifiable). Record it in every doc you write today.
4. Read `STATUS.md`, ALL `live/*.md`, latest `00_summary/implementation-summary-*.md`.
5. Create `live/sess-<id>.md` from `live/_template.md`; heartbeats + scope + touched register it.
6. Read the relevant `.agents/rules/` file + the `.agents/skills/*/SKILL.md` for the task.
7. Verify baseline: `pwsh workflow/neutral/verify.ps1` (take the lock first — see protocol §4).
8. Then and only then start the task.

## During work (every turn)

- `pwsh workflow/neutral/coordination.ps1 -Session <sid> -Heartbeat`
- `… -Lessons` (read the failure-pattern registry BEFORE acting)
- Zone overlap? Stop → post on `live/board.md` → take another task.
- Turn end: `… -Lesson "<failure -> root cause -> remedy -> proof>"` — exactly one line, no empty turns.

## Wave close-out

1. Summary → `00_summary/implementation-summary-<ts>-<sid>.md` per `00_summary/_template.md`
   (must end with `## Lessons / Process Improvements`).
2. Append one line to `STATUS.md` (append-only; never edit rows).
3. Update board-row Status cells only if a board row exists (cross-session only).
4. `pwsh workflow/neutral/coordination.ps1 -Session <sid> -CloseOut` — fail-closed, blocks staging.
5. Stage by explicit paths → `git diff --cached --stat` → user approval → commit.

## Zone map

`zones.md` in this folder is the **single authority** — coordination.ps1 parses it. Keep your
session's ownership lines there, not in this readme.

## Definition of Done (every feature)

- [ ] Service/endpoint exists — no invented DTOs/endpoints
- [ ] Tests assert the specific behavior, not "renders without error"
- [ ] Target project builds 0 warnings; suites green
- [ ] Docs written this session use the freshly-verified identity + real timestamp
- [ ] `live/sess-<id>.md` updated (current task, touched files, heartbeat)
- [ ] Wave close-out complete: summary + STATUS row + `-CloseOut` passed