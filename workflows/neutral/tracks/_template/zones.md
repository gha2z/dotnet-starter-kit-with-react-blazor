# <track> — zones.md (ownership authority)

> The ONLY place the zone→session map lives. `coordination.ps1` parses this file (format below)
> and fails closed on any path outside the running session's zones. Owners are session ids
> WITHOUT the `sess-` prefix. Keep this in sync with the AGENTS.md scope notes of your repo.
>
> Format: one line per zone — `owner: path-prefix` (spaces around `:` optional, `#` = comment,
> prefix matching is recursive). Example: `main: src/Modules/Catalog`.

# owner: prefix
main: src/
main: clients/admin
main: clients/dashboard
main: .agents
parity: clients/*-blazor
docs: workflow/