# sess-main

identity: opencode/x-preview-f-free (verified fresh 2026-08-26) | started: 2026-08-06 | state: active | heartbeat: 2026-08-26 18:40

## Current focus

Idle — Phase-10-Detail-Behavior-Parity complete (2026-08-26). Next mission awaits a human
`spec.md` in a new `Phase-11-*` folder. Mission state lives in each mission folder
(`{spec,plan,implementation}.md`) — NOT in this file.

## Completed missions (evidence in mission folders + 00_summary/)

- Phases 0–8 (foundation → parity audit) — see `STATUS.md` ledger + phase folders.
- Chat parity wave (2026-08-23, `e45e00de`…`478d6a5d`): SignalR double-start root cause,
  avatars, toasts, settings gating, mobile pane — probe 16/0.
- Gap hunt C1–C4 (2026-08-24, `32944917`…`fe75af91`): channel lifecycle + role gating,
  CRUD harnesses (dashboard 37/0 + admin 21/0), inactivity wiring, impersonation E2E,
  cross-tenant search fix — thorough-qa 48/0.

## Lessons (recent, high-value — full ledger: `live/lessons.md`)

- Non-Immediate MudTextField binds on change(=blur): Playwright fill() must blur before save/search.
- Delegating-handler TryAddWithoutValidation breaks explicit per-request header overrides.
- MudBlazor 9: MudDialog CloseOnEscapeKey defaults false; MudAvatar has no Image param.
- SignalR client wrappers must never blind-rebuild the connection (handlers orphan silently).
- Never send Authorization headers to presigned S3 URLs — fetch via plain-browser fetch; any injected header breaks SigV4 (400).

Next: idle — awaiting human Phase-11 spec
