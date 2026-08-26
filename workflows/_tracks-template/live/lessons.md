# Lessons — <track-name>

> Failure-pattern registry + append-only ledger.
> 1. At the top: the **failure-pattern registry** (permanent). Tag each pattern class.
> 2. Below: the **event ledger** (every turn, exactly one line).
> Format: `timestamp :: failure -> root cause -> remedy -> proof`
> If a new event matches a known pattern, append with `[RECUR]` + the standing remedy.
> Patterns are never re-litigated.

# [BUILD] build warnings creep in -> repo convention is 0 warnings -> fix before staging -> dotnet build 0/0
# [TEST] bUnit cannot fire MudBlazor onchange handlers directly -> use _comp.InvokeAsync() -> bUnit docs
# [PATH] node driver fails when script/evidence dirs aren't absolute paths -> always run driver with absolute paths
# [EVAL] scripts that LOAD a model's eval system get blocked -> ship standalone logic without eval wiring
# [BATCH] parallel tool batches after failures escalate the PRM circuit breaker -> after any failure, drop to single sequential tool calls
