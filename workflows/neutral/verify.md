# Verification Contract — Original repo (neutral)

> Non-negotiable gates for the original fullstackhero repo (React pair + .NET monolith).
> Numbers in docs come from a real run **this session** — never from memory. `verify.ps1`
> (track template) mechanical-izes the handoff gate; it deletes `obj/`/`bin/` — take the verify
> lock first (`coordination.ps1 -LockVerify` / `-UnlockVerify`).

## Gates

| # | Gate | Command |
|---|---|---|
| 1 | Backend build, 0 warnings (warnings ARE errors) | `dotnet build src/FSH.Starter.slnx` |
| 2 | Backend tests (xUnit; integration needs Docker) | `dotnet test src/FSH.Starter.slnx` |
| 3 | React type + build | `cd clients/admin && npm run build && npx tsc --noEmit` (same for `clients/dashboard`) |
| 4 | React tests (route-mocked Playwright) | per-app script (`npm run test`/Playwright project) |
| 5 | Migrations current | `dotnet run --project src/Host/FSH.Starter.DbMigrator -- list-pending` (must be empty) |
| 6 | Module boundaries / conventions | `dotnet test src/Tests/Architecture.Tests` |
| 7 | Secrets / config | never commit secrets; `.env` stays out of git |

## Cadence

- After each unit of work: `dotnet build <target.csproj>` then `dotnet test <target>.Tests`.
- Phase end: full solution build + both app suites + Architecture.Tests.
- `clients/admin` and `clients/dashboard` are the **reference React pair** — the AGENTS.md golden
  rules treat them as frozen parity sources on the original repo too: read-only unless the user
  explicitly instructs otherwise.

## Lock etiquette

`coordination.ps1 -LockVerify` before, `-UnlockVerify` after any clean/build/test that clears
`obj/`/`bin/` in a shared checkout. Another session holding the lock = wait or take other work.
Stale lock (owner heartbeat > 12h): announce on `live/board.md`, then remove.