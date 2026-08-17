# verify.ps1 — neutral handoff gate (original repo)

# Deletes obj/ and bin/ (source-gen caches are NOT flushed by --no-incremental), builds the
# backend with warning counting, runs the test projects, type-checks + builds both React apps,
# and prints a paste-ready verification block for the wave summary.
# Usage: pwsh verify.ps1 [-SkipClean] [-SkipTests] [-SkipFrontend]
# Run from the repo root. Take the verify lock first in a shared checkout:
#   pwsh workflow/neutral/coordination.ps1 -Session <sid> -LockVerify
#   ... pwsh workflow/neutral/coordination.ps1 -Session <sid> -UnlockVerify

[CmdletBinding()]
param(
    [switch]$SkipClean,
    [switch]$SkipTests,
    [switch]$SkipFrontend
)

$ErrorActionPreference = 'Continue'
$root = $PWD.Path
$started = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
$report = @()

if (-not $SkipClean) {
    Write-Host "=== verify: cleaning obj/ bin/ ===" -ForegroundColor Cyan
    Get-ChildItem -Path $root -Recurse -Directory -Name -ErrorAction SilentlyContinue |
        Where-Object { $_ -match '\\?(obj|bin)$' -and $_ -notmatch 'node_modules|\\\.' } |
        ForEach-Object {
            $p = Join-Path $root $_
            Remove-Item $p -Recurse -Force -ErrorAction SilentlyContinue
        }
    Write-Host "  cleaned."
}

Write-Host "=== verify: backend build (slnx) ===" -ForegroundColor Cyan
$b = dotnet build "$root/src/FSH.Starter.slnx" --nologo 2>&1
$b | Select-Object -Last 5
$warnCount = ([regex]::Matches(($b -join "`n"), 'warning')).Count
$errCount = ([regex]::Matches(($b -join "`n"), 'error')).Count
$report += "backend build: warnings=$warnCount errors=$errCount"

if (-not $SkipTests) {
    Write-Host "=== verify: backend tests ===" -ForegroundColor Cyan
    $t = dotnet test "$root/src/FSH.Starter.slnx" --no-build --nologo 2>&1
    $t | Select-Object -Last 5
    $report += "backend tests: $(([regex]::Matches(($t -join "`n"), 'Passed!')).Count) passed, $(([regex]::Matches(($t -join "`n"), 'Failed!')).Count) failed"

    Write-Host "=== verify: architecture tests ===" -ForegroundColor Cyan
    $a = dotnet test "$root/src/Tests/Architecture.Tests" --nologo 2>&1
    $a | Select-Object -Last 3
    $report += "architecture tests: $(([regex]::Matches(($a -join "`n"), 'Passed!')).Count) passed, $(([regex]::Matches(($a -join "`n"), 'Failed!')).Count) failed"
}

if (-not $SkipFrontend) {
    foreach ($app in 'admin', 'dashboard') {
        Write-Host "=== verify: React $app (tsc + build) ===" -ForegroundColor Cyan
        Push-Location "$root/clients/$app"
        $tsc = npx tsc --noEmit 2>&1
        $tscCount = @($tsc | Where-Object { $_ -match 'error' }).Count
        $r = npm run build 2>&1
        $r | Select-Object -Last 3
        Pop-Location
        $report += "react $app: tsc errors=$tscCount buildOk=$($LASTEXITCODE -eq 0)"
    }
}

Write-Host "`n=== VERIFICATION BLOCK (paste into the wave summary) ===" -ForegroundColor Green
Write-Host ("- Run: verify.ps1 @ {0}" -f $started)
$report | ForEach-Object { Write-Host "- $_" }