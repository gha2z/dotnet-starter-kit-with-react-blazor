# SQLitePCLRaw Vulnerability Advisory (GHSA-2m69-gcr7-jv3q)

## Summary
- **CVE**: N/A (GitHub Security Advisory only)
- **GHSA**: GHSA-2m69-gcr7-jv3q
- **Severity**: High
- **Affected Package**: `SQLitePCLRaw.lib.e_sqlite3` and `SQLitePCLRaw.lib.e_sqlite3.android`
- **Affected Version**: 2.1.11 and earlier
- **Fixed Version**: 2.1.12 — **RESOLVED** (2026-08-13)

## Description
A vulnerability exists in SQLitePCLRaw versions 2.1.11 and earlier that could allow remote code execution under certain conditions. The vulnerability affects the bundled e_sqlite3 library used across Android, iOS, macCatalyst, and Windows platforms in the FSH Hybrid application.

## Current Status
**Resolved.** The patched release (`SQLitePCLRaw.lib.e_sqlite3` 2.1.12) is now published on NuGet.org. It is pinned in `src/Directory.Packages.props` (transitive pinning), so all consumers —
including `Microsoft.EntityFrameworkCore.Sqlite` (Framework.Tests) — resolve the patched build and NuGetAudit no longer trips under `TreatWarningsAsErrors`.

## Mitigation
1. The patched package is pinned centrally in `src/Directory.Packages.props`.
2. Re-run `dotnet restore` + `dotnet build` to confirm zero NU1903 findings for SQLitePCLRaw.

## References
- GitHub Advisory: https://github.com/advisories/GHSA-2m69-gcr7-jv3q
- SQLitePCLRaw Project: https://github.com/sqlpcl/raw