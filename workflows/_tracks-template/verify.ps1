# verify.ps1 — Default verification script for tracks.
# Replaced by track-specific verify.ps1 when needed (e.g. Blazor WASM apps).
# Clean build + full tests, always from a clean state.

param([string]$TrackRoot)

$ErrorActionPreference = 'Stop'
$started = Get-Date

function Write-Step([string]$t) { Write-Host "`n=== $t ===" -ForegroundColor Cyan }
function Write-Ok([string]$m) { Write-Host "  OK - $m" -ForegroundColor Green }
function Write-Fail([string]$m) { Write-Host "  FAIL - $m" -ForegroundColor Red }

Write-Host "=== Verification run ($(Get-Date -Format 'yyyy-MM-dd HH:mm')) ===" -ForegroundColor Cyan

# 1. Clean
Write-Step "Clean"
Get-ChildItem 'src' -Recurse -Directory -Filter 'bin' | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem 'src' -Recurse -Directory -Filter 'obj' | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Write-Ok "cleaned bin/obj"

# 2. Build
Write-Step "Build"
$slnx = Get-ChildItem 'src' -Filter '*.slnx' | Select-Object -First 1
if (-not $slnx) { Write-Fail "no .slnx found"; exit 1 }
$build = dotnet build $slnx.FullName 2>&1
$errs = @($build | Where-Object { $_ -match ': error ' }).Count
$warns = @($build | Where-Object { $_ -match ': warning ' }).Count
Write-Host "  build: $errs errors / $warns warnings"
if ($errs -gt 0) { Write-Fail "build failed"; exit 1 }

# 3. Tests
Write-Step "Tests"
$testProjects = Get-ChildItem 'src' -Recurse -Filter '*.Tests.csproj' -ErrorAction SilentlyContinue
$totalPassed = 0; $totalFailed = 0
foreach ($tp in $testProjects) {
    $result = dotnet test $tp.FullName --no-build --verbosity quiet 2>&1
    $passed = ([regex]::Matches($result, 'Passed: \d+') | ForEach-Object { [int]($_ -replace 'Passed: ','') } | Measure-Object -Sum).Sum
    $failed = ([regex]::Matches($result, 'Failed: \d+') | ForEach-Object { [int]($_ -replace 'Failed: ','') } | Measure-Object -Sum).Sum
    $totalPassed += $passed; $totalFailed += $failed
    Write-Host "  $($tp.Directory.Name): $passed passed / $failed failed"
}
Write-Host "`n  TOTAL: $totalPassed passed / $totalFailed failed"
if ($totalFailed -gt 0) { Write-Fail "tests failed"; exit 1 }

# Summary
$elapsed = [math]::Round(((Get-Date) - $started).TotalSeconds, 1)
Write-Host "`n=== VERIFICATION PASSED ($elapsed s) ===" -ForegroundColor Green
Write-Host "  errors: $errs | warnings: $warns | tests: $totalPassed/$($totalPassed + $totalFailed)"
exit 0
