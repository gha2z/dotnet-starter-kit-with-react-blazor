# zones.md — zone→owner map (v2 authority; parsed by workflows/current/coordination.ps1)
#
# Converted 2026-08-15 from the v1 hardcoded map (AGENTIC-GUIDE §3.3 + readme "Scope Restriction").
# Single authority for ownership. Owners are session ids WITHOUT the "sess-" prefix.
# Format: `owner: path-prefix` (# = comment, prefix matching is recursive).
# Lines that are shared in v1 (announce on board.md before editing) are kept under `main:`
# with a comment marking them shared-so-far.

maui: clients/FSH.Hybrid
maui: verify-hybrid.ps1

main: clients/dashboard-blazor
main: clients/BlazorShared
main: clients/admin-blazor
main: opencode
main: workflows
main: AGENTS.md
main: CLAUDE.md
main: GEMINI.md
main: docs/spec        # shared (announce first)
main: .agents/rules/frontend   # shared (announce first)