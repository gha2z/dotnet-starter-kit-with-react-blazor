# verify.ps1 — Handoff gate for the Blazor frontends.
# Clean obj/bin -> build both WASM apps (count warnings/errors) -> run both bUnit suites -> icon audit.
# Output is a ready-to-paste "Verification" block for the implementation summary.
# Usage: pwsh opencode/addBlazorFrontends/verify.ps1 [-SkipClean] [-SkipTests]

param([switch]$SkipClean, [switch]$SkipTests)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$apps = @(
    @{ Name = 'dashboard'; Proj = "clients\dashboard-blazor\FSH.Dashboard.Wasm\FSH.Dashboard.Wasm.csproj"; Tests = "clients\dashboard-blazor\FSH.Dashboard.Wasm.Tests" },
    @{ Name = 'admin';    Proj = "clients\admin-blazor\FSH.Admin.Wasm\FSH.Admin.Wasm.csproj";       Tests = "clients\admin-blazor\FSH.Admin.Wasm.Tests" }
)

Write-Host "=== FSH Blazor Verify (root: $root) ===" -ForegroundColor Cyan

if (-not $SkipClean) {
    Write-Host "--- Cleaning obj/bin ---"
    foreach ($dir in @('clients\dashboard-blazor\FSH.Dashboard.Wasm', 'clients\dashboard-blazor\FSH.Dashboard.Wasm.Tests',
                       'clients\admin-blazor\FSH.Admin.Wasm', 'clients\admin-blazor\FSH.Admin.Wasm.Tests')) {
        foreach ($sub in @('obj', 'bin')) {
            $p = Join-Path $root (Join-Path $dir $sub)
            if (Test-Path $p) { Remove-Item -Recurse -Force $p }
        }
    }
    Write-Host "  cleaned."
}

$summary = @()
foreach ($app in $apps) {
    Write-Host "--- Building $($app.Name)-blazor ---"
    $build = dotnet build (Join-Path $root $app.Proj) 2>&1
    $errs  = @($build | Where-Object { $_ -match ': error ' }).Count
    $warns = @($build | Where-Object { $_ -match ': warning ' }).Count
    $summary += "$($app.Name)-blazor build: $errs errors / $warns warnings"
    if ($errs -gt 0) { Write-Host "BUILD FAILED ($errs errors)" -ForegroundColor Red; $build | Where-Object { $_ -match ': error ' } | ForEach-Object { Write-Host $_ }; exit 1 }

    if (-not $SkipTests) {
        Write-Host "--- Building $($app.Name)-blazor tests ---"
        $tbuild = dotnet build (Join-Path $root $app.Tests) 2>&1
        $terrs  = @($tbuild | Where-Object { $_ -match ': error ' }).Count
        $twarns = @($tbuild | Where-Object { $_ -match ': warning ' }).Count
        $summary += "$($app.Name)-blazor tests build: $terrs errors / $twarns warnings"
        if ($terrs -gt 0) { Write-Host "TEST BUILD FAILED ($terrs errors)" -ForegroundColor Red; $tbuild | Where-Object { $_ -match ': error ' } | ForEach-Object { Write-Host $_ }; exit 1 }

        Write-Host "--- Testing $($app.Name)-blazor ---"
        $test = dotnet test (Join-Path $root $app.Tests) --no-build 2>&1
        $m = $test | Where-Object { $_ -match 'Passed!|Failed!' } | Select-Object -Last 1
        $totals = $test | Where-Object { $_ -match 'Total:' } | Select-Object -Last 1
        $summary += "$($app.Name)-blazor tests: $($m -replace '.*?(Passed!|Failed!)','$1') $totals"
        Write-Host $m; Write-Host $totals
    }
}

Write-Host "--- Icon audit (MudBlazor Icons.Material.*) ---"
$mudDll = Get-ChildItem (Join-Path $root 'clients\dashboard-blazor\FSH.Dashboard.Wasm\bin') -Recurse -Filter 'MudBlazor.dll' | Select-Object -First 1
if (-not $mudDll) { Write-Host 'MudBlazor.dll not found - skipping icon audit' -ForegroundColor Yellow }
else {
    $asm = [System.Reflection.Assembly]::LoadFrom($mudDll.FullName)
    $t = $asm.GetType('MudBlazor.Icons+Material')
    $variants = @('Filled','Outlined','Rounded','Sharp','TwoTone')
    $bad = @()
    Get-ChildItem (Join-Path $root 'clients') -Recurse -Include '*.razor','*.cs' |
        Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } |
        ForEach-Object {
            $content = Get-Content $_.FullName -Raw
            foreach ($v in $variants) {
                [regex]::Matches($content, "Icons\.Material\.$v\.([A-Za-z0-9_]+)") | ForEach-Object {
                    $name = $_.Groups[1].Value
                    $vt = $asm.GetType("MudBlazor.Icons+Material+$v")
                    if ($vt -and -not $vt.GetField($name)) { $bad += "$($_.FullName): Icons.Material.$v.$name" }
                }
            }
        }
    if ($bad.Count -eq 0) { $summary += 'icon audit: PASS (all Icons.Material.* resolve)' }
    else { $summary += "icon audit: FAIL ($($bad.Count) unresolved)"; $bad | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow } }
}

Write-Host "`n=== VERIFICATION (paste into summary) ===" -ForegroundColor Green
$summary | ForEach-Object { Write-Host $_ }
