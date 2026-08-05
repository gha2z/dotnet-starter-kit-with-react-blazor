# Live Session Coordination

Each running session maintains ONE file here: `sess-<session-id>.md` (single-writer).
`board.md` is the shared, append-only request board. `_template.md` is the skeleton.

Protocol: see the **"Multi-Session Coordination Protocol"** section in `../readme.md`.

Rules in one line each:

- Write only your own `sess-<id>.md`; append (never rewrite) rows to `board.md`.
- Re-read all files fresh: session start → before every task → before staging → before merging.
- Claim tasks by appending `— claimed by <sid> @ <yyyy-MM-dd HH:mm>` to the plan-file task line.
- Touch only files inside your owned zones (ownership map in the readme).
- Stage by explicit path; never `git add -A` in the shared checkout.
- Stamp your `heartbeat:` on every task boundary; claims with a stale heartbeat (>12h) are void
  and get re-assigned by the user.
- Announce "running verify" on `board.md` before starting `verify.ps1` (it builds both WASM apps
  from the shared checkout).
