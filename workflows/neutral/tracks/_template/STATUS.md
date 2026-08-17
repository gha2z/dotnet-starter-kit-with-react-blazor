# <track> — STATUS & Coordination Ledger

> Append-only progress ledger — the human's one-line window into the work-stream and the
> compaction-proof state for any session. **Never edit existing rows; append new ones.**

- **Last Update:** <yyyy-MM-dd HH:mm> — by: <tool> (model: <verified modelID or (unresolved)>)
- **Repo:** fullstackhero/dotnet-starter-kit · **Track root:** <workflow-dir>/<track>/
- **Active sessions:** sess-<id> (zone: <zones.md owner lines>) · …
- **Next task:** <from 00-Index.md / plan.md>

---

## Ledger

| When | Who | Wave / task | What | Verification | Commit |
|---|---|---|---|---|---|
| <yyyy-MM-dd HH:mm> | sess-<id> | <plan task id> | <one line> | <build/test numbers> | <sha/—> |
| … | | | | | |

---

## Active Streams / Sessions

| Session | Zone (zones.md) | State | Heartbeat |
|---|---|---|---|
| sess-<id> | <prefixes> | active/awaiting user/idle | <yyyy-MM-dd HH:mm> |

(Each session keeps its own detailed file in `live/`; this table is the at-a-glance view.)