# live/ — session coordination

Protocol quick reference — the canonical rules live in `workflow/neutral/session-protocol.md`.

| File | Who writes | What |
|---|---|---|
| `board.md` | anyone (append-only rows) | cross-session requests & handoffs |
| `lessons.md` | everyone, one line per turn | lesson ledger + failure-pattern registry (rule: no empty turns) |
| `sess-<id>.md` | that session only | **single-writer** — others may create a stub from `_template.md`, never populate it |
| `locks/verify.lock` | coordination.ps1 | shared build/verify lock (rule: lock, don't announce) |

Read all files fresh before every task, before staging, before merging. Files are UTF-8 — always
read/write as UTF-8. If a board row was lost to a concurrent append, re-append the SAME row ID
with `(re-appended)`.