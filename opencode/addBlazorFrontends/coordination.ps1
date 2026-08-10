# coordination.ps1 — Mechanical gate for the multi-session coordination protocol.
# See opencode/addBlazorFrontends/readme.md "Coordination Gate Script" + "Multi-Session
# Coordination Protocol". Turns rules 1/7/8/9 into checks a session cannot forget.
#
# Usage:
#   pwsh coordination.ps1 -Session <sid> -Start          # validate session file + stamp heartbeat
#   pwsh coordination.ps1 -Session <sid> -Heartbeat      # stamp heartbeat (every user turn)
#   pwsh coordination.ps1 -Session <sid> -Gate           # ownership + touched-files + heartbeat + lock checks
#   pwsh coordination.ps1 -Session <sid> -LockVerify     # create live/locks/verify.lock (fail if exists)
#   pwsh coordination.ps1 -Session <sid> -UnlockVerify   # remove live/locks/verify.lock
#   pwsh coordination.ps1 -Session <sid> -LockCheck      # report lock state only
#
# Exit code 1 on any violation. Run from the repo root.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Session,
    [switch]$Start,
    [switch]$Heartbeat,
    [switch]$Gate,
    [switch]$LockVerify,
    [switch]$UnlockVerify,
    [switch]$LockCheck,
    [int]$StaleHours = 12
)

$ErrorActionPreference = 'Stop'
$Session = $Session -replace '^sess-', ''
$root = $PWD.Path
$liveDir = Join-Path $root 'opencode\addBlazorFrontends\live'
$locksDir = Join-Path $liveDir 'locks'
$lockFile = Join-Path $locksDir 'verify.lock'
$sessionFile = Join-Path $liveDir "sess-$Session.md"
$failures = @()

# ---------------------------------------------------------------------------
# Zone map — MUST stay in sync with the readme "Scope Restriction" section.
# Paths are relative to the repo root; a path is owned if it starts with any
# of the zone's prefixes.
# ---------------------------------------------------------------------------
$zoneMap = [ordered]@{
    'sess-main' = @(
        'clients/dashboard-blazor/',
        'clients/BlazorShared/',
        'clients/admin-blazor/',
        'STATUS.md', '00-Index.md', '.gitignore',
        'AGENTS.md', '.agents/workflows/',
        'opencode/addBlazorFrontends/',
        'HUMAN-GUIDE.md', 'docs/spec/',
        'opencode/AGENTIC-GUIDE.md', 'opencode/_tracks-template/'
    )
    'sess-maui' = @(
        'clients/FSH.Hybrid/',
        'opencode/addBlazorFrontends/verify-hybrid.ps1',
        'opencode/addBlazorFrontends/Phase-05-MAUI-Hybrid/',
        '.agents/rules/frontend/maui-hybrid.md'
    )
    'shared' = @(
        '.agents/rules/frontend/',
        'README.md'
    )
}

function Get-Heartbeat([string]$file) {
    if (-not (Test-Path $file)) { return $null }
    $line = Get-Content $file | Where-Object { $_ -match 'heartbeat:' } | Select-Object -First 1
    if (-not $line) { return $null }
    $m = [regex]::Match($line, 'heartbeat:\s*([0-9]{4}-[0-9]{2}-[0-9]{2}\s+[0-9]{2}:[0-9]{2})')
    if (-not $m.Success) { return $null }
    return [datetime]::ParseExact($m.Groups[1].Value, 'yyyy-MM-dd HH:mm', $null)
}

function Stamp-Heartbeat([string]$file) {
    $now = Get-Date -Format 'yyyy-MM-dd HH:mm'
    $content = Get-Content $file -Raw -Encoding UTF8
    if ($content -match 'heartbeat:.*') {
        $content = [regex]::Replace($content, 'heartbeat:[^\r\n]*', "heartbeat: $now", 1)
    }
    Set-Content -Path $file -Value $content -Encoding UTF8 -NoNewline
    Write-Host "  heartbeat stamped: $now (in sess-$Session.md)"
}

function Test-OwnsPath([string]$path) {
    $normalized = $path.Replace('\', '/')
    if (-not $zoneMap.Contains($Session)) {
        return $true # unknown session: don't block on ownership, but warn
    }
    foreach ($prefix in $zoneMap[$Session]) {
        $p = $prefix.Replace('\', '/')
        if ($normalized.StartsWith($p, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    }
    return $false
}

function Get-TouchedFilesFromOthers {
    $touched = @()
    foreach ($f in Get-ChildItem $liveDir -Filter 'sess-*.md' -File) {
        if ($f.BaseName -eq "sess-$Session") { continue }
        $lines = Get-Content $f.FullName
        $inTouched = $false
        foreach ($line in $lines) {
            if ($line -match '^##\s+Touched') { $inTouched = $true; continue }
            if ($line -match '^##\s+') { $inTouched = $false }
            if ($inTouched -and $line -match '^\s*-\s+(.+)') {
                $touched += $Matches[1].Trim().TrimStart('*', '`').Trim()
            }
        }
    }
    return $touched
}

function Test-PathTouchedByOthers([string]$path) {
    $normalized = $path.Replace('\', '/')
    # Shared coordination files (per-file ownership) are exempt from the overlap check.
    $shared = @(
        "opencode/addBlazorFrontends/live/sess-$Session.md",
        'opencode/addBlazorFrontends/live/board.md',
        'opencode/addBlazorFrontends/live/README.md',
        'opencode/addBlazorFrontends/live/_template.md',
        'opencode/addBlazorFrontends/live/locks/'
    )
    foreach ($s in $shared) {
        if ($normalized -eq $s -or $normalized.StartsWith($s, [StringComparison]::OrdinalIgnoreCase)) { return $null }
    }
    foreach ($t in Get-TouchedFilesFromOthers) {
        $tn = $t.Replace('\', '/')
        if ($tn.EndsWith('/')) {
            if ($normalized.StartsWith($tn, [StringComparison]::OrdinalIgnoreCase)) { return $t }
        }
        elseif ($normalized -eq $tn -or $normalized.StartsWith("$tn/", [StringComparison]::OrdinalIgnoreCase)) {
            return $t
        }
    }
    return $null
}

# ---------------------------------------------------------------------------

if (-not (Test-Path $liveDir)) {
    Write-Host "ERROR: $liveDir does not exist - are you in the repo root?" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $sessionFile)) {
    Write-Host "ERROR: no session file live/sess-$Session.md - create one from live/_template.md first (rule 1)." -ForegroundColor Red
    exit 1
}

if ($Start) {
    Write-Host "=== coordination: session start ($Session) ==="
    Stamp-Heartbeat $sessionFile
    Write-Host "  OK - session file present. Read live/*.md fresh, then claim a task in your zone."
}

if ($Heartbeat) {
    Stamp-Heartbeat $sessionFile
}

if ($LockVerify) {
    if (Test-Path $lockFile) {
        Write-Host "ERROR: verify.lock already exists - another session is running verify (rule 9)." -ForegroundColor Red
        Write-Host "  ($(Get-Content $lockFile -Raw -ErrorAction SilentlyContinue))"
        exit 1
    }
    New-Item -ItemType Directory -Path $locksDir -Force | Out-Null
    "$Session @ $(Get-Date -Format 'yyyy-MM-dd HH:mm')" | Set-Content $lockFile -Encoding UTF8
    Write-Host "  lock taken: $lockFile"
}

if ($UnlockVerify) {
    if (Test-Path $lockFile) {
        Remove-Item $lockFile -Force
        Write-Host "  lock released: $lockFile"
    }
    else {
        Write-Host "  no lock present - nothing to release."
    }
}

if ($LockCheck) {
    if (Test-Path $lockFile) {
        Write-Host "  lock HELD by: $(Get-Content $lockFile -Raw -ErrorAction SilentlyContinue)"
    }
    else {
        Write-Host "  lock free."
    }
}

if ($Gate) {
    Write-Host "=== coordination: gate ($Session) ==="

    $hb = Get-Heartbeat $sessionFile
    if ($null -eq $hb) {
        $failures += 'session file has no parseable heartbeat - run -Start first'
    }
    elseif (((Get-Date) - $hb).TotalHours -gt $StaleHours) {
        $failures += "heartbeat stale (>$StaleHours h, last: $($hb.ToString('yyyy-MM-dd HH:mm'))) - stamp with -Heartbeat"
    }
    else {
        Write-Host "  heartbeat OK ($($hb.ToString('yyyy-MM-dd HH:mm')))."
    }

    if (Test-Path $lockFile) {
        $lockOwner = Get-Content $lockFile -Raw -ErrorAction SilentlyContinue
        if ($lockOwner -notmatch $Session) {
            $failures += "verify.lock is held by another session ($lockOwner) - do not build/test shared projects (rule 9)"
        }
        else {
            Write-Host "  verify.lock held by this session - OK."
        }
    }
    else {
        Write-Host "  verify.lock free."
    }

    Write-Host "  -- worktree ownership check (uncommitted + staged) --"
    $status = git status --porcelain
    foreach ($line in $status) {
        if ($line -match '^\?\?\s+(.+)$') {
            $path = $Matches[1].TrimEnd('/')
        }
        # Two-column porcelain status ("XY path"): X=index, Y=worktree.
        # Covers "M  p" (staged), " M p" (unstaged-only), "MM p" (both),
        # "AM p", "R  old -> new" (rename), "T p" / "U p" etc. The old
        # '^[MARD]\s+' pattern missed the leading-space " M" form — an
        # unstaged change outside your zones silently passed the gate.
        elseif ($line -match '^(.{2})\s+(.+)$') {
            $path = $Matches[2]
            # Rename/copy lines carry "old -> new"; check the worktree target.
            if ($path -match '^.+\s+->\s+(.+)$') { $path = $Matches[1] }
        }
        elseif ($line -match '^\S+\s+(.+)$') {
            $path = $Matches[1]
        }
        else { continue }
        if ($path -eq '') { continue }
        if (-not (Test-OwnsPath $path)) {
            $failures += "path outside your zones: $path"
        }
    }

    Write-Host "  -- touched-files overlap check (staged + untracked) --"
    foreach ($line in $status) {
        if ($line -match '^\?\?\s+(.+)$') {
            $path = $Matches[1].TrimEnd('/')
        }
        elseif ($line -match '^(.{2})\s+(.+)$') {
            $path = $Matches[2].TrimEnd('/')
            if ($path -match '^.+\s+->\s+(.+)$') { $path = $Matches[1] }
        }
        else { continue }
        if (-not $path) { continue }
        $hit = Test-PathTouchedByOthers $path
        if ($hit) {
            $failures += "staged/untracked path '$path' is in another session's touched register ('$hit') - stop and coordinate on board.md"
        }
    }

    if ($failures.Count -gt 0) {
        Write-Host "`nGATE FAILED:" -ForegroundColor Red
        $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
        exit 1
    }
    Write-Host "`nGATE PASSED - safe to stage/proceed." -ForegroundColor Green
}
