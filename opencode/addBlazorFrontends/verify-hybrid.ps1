# verify-hybrid.ps1 — Handoff gate for the MAUI Hybrid app (Phase 5).
# Builds the Windows + Android TFMs (count warnings/errors), then runs the FSH.Hybrid.Tests suite.
# Output is a ready-to-paste "Verification" block for the implementation summary.
# NOTE: the .github/** MAUI CI workflow is deferred; this script is the local replacement.
# Usage: pwsh opencode/addBlazorFrontends/verify-hybrid.ps1 [-SkipClean] [-SkipTests]

param([switch]$SkipClean, [switch]$SkipTests)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$proj = Join-Path $root 'clients\FSH.Hybrid\FSH.Hybrid\FSH.Hybrid.csproj'
$testsProj = Join-Path $root 'clients\FSH.Hybrid\FSH.Hybrid.Tests\FSH.Hybrid.Tests.csproj'

Write-Host "=== FSH MAUI Hybrid Verify (root: $root) ===" -ForegroundColor Cyan

if (-not $SkipClean) {
    Write-Host "--- Cleaning obj/bin ---"
    foreach ($dir in @('clients\FSH.Hybrid\FSH.Hybrid', 'clients\FSH.Hybrid\FSH.Hybrid.Tests')) {
        foreach ($sub in @('obj', 'bin')) {
            $p = Join-Path $root (Join-Path $dir $sub)
            if (Test-Path $p) { Remove-Item -Recurse -Force $p }
        }
    }
    Write-Host "  cleaned."
}

$summary = @()

foreach ($tfm in @('net10.0-windows10.0.19041.0', 'net10.0-android')) {
    Write-Host "--- Building hybrid ($tfm) ---"
    $build = dotnet build $proj -f $tfm 2>&1
    $errs  = @($build | Where-Object { $_ -match ': error ' }).Count
    $warns = @($build | Where-Object { $_ -match ': warning ' }).Count
    $summary += "hybrid ($tfm) build: $errs errors / $warns warnings"
    if ($errs -gt 0) {
        Write-Host "BUILD FAILED (${tfm}: $errs errors)" -ForegroundColor Red
        $build | Where-Object { $_ -match ': error ' } | ForEach-Object { Write-Host $_ }
        exit 1
    }
}

if (-not $SkipTests) {
    Write-Host "--- Testing FSH.Hybrid.Tests ---"
    $test = dotnet test $testsProj 2>&1
    $errs = @($test | Where-Object { $_ -match ': error ' }).Count
    if ($errs -gt 0) { Write-Host "TEST FAILED ($errs build errors)" -ForegroundColor Red; exit 1 }
    $m = $test | Where-Object { $_ -match 'Passed!|Failed!' } | Select-Object -Last 1
    $summary += "hybrid tests: $m"
    Write-Host $m
    if ($m -match 'Failed!') { exit 1 }
}

Write-Host "`n=== VERIFICATION (paste into summary) ===" -ForegroundColor Green
$summary | ForEach-Object { Write-Host $_ }
