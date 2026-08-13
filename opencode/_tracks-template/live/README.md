# <track-name> — live session state

> Session coordination state for this track. Mirrors the working `addBlazorFrontends/live/` layout.

```
live/
├── board.md          # cross-track coordination: announcements, locks, gates
├── locks/            # verify.lock etc. (heartbeat-guarded)
└── <sid>.md           # one session file per active session
```

## Cadence

- `coordination.ps1 -Session <sid> -Heartbeat` every turn.
- `-Gate` before staging anything.
- Lock (`-LockVerify` / `-UnlockVerify`) before running the verify gate.
- Stale lock (owner heartbeat > 12h) → announce on `board.md`, then remove.

## Board content

| Session | Zone | Heartbeat | Gate | Status |
|---|---|---|---|---|
| `sess-<track>-1` | … | … | On/Off | planning / coding / verifying / staged-waiting-approval |