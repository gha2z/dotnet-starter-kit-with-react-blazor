# coordination.ps1 — Mechanical gate for the multi-session coordination protocol. v2 (generic).
# Neutral: pure PowerShell + git — no opencode, no plugins. Zone ownership is read from
# <TrackRoot>/zones.md (the single authority) — no script/map drift. See session-protocol.md.
# Turns the heartbeat / ownership / lock / lessons / close-out rules into checks a session
# cannot forget.
#
# Usage:
#   pwsh coordination.ps1 -Session <sid> -Start          # validate session file + stamp heartbeat
#   pwsh coordination.ps1 -Session <sid> -Heartbeat      # stamp heartbeat (every user turn)
#   pwsh coordination.ps1 -Session <sid> -Gate           # ownership + touched-files + heartbeat + lock checks
#   pwsh coordination.ps1 -Session <sid> -LockVerify     # create live/locks/verify.lock (fail if exists)
#   pwsh coordination.ps1 -Session <sid> -UnlockVerify   # remove live/locks/verify.lock
#   pwsh coordination.ps1 -Session <sid> -LockCheck      # report lock state only
#   pwsh coordination.ps1 -Session <sid> -CloseOut       # wave close-out gate (summary + Lessons + STATUS)
#   pwsh coordination.ps1 -Session <sid> -Lessons        # print the lessons-ledger tail (turn start)
#   pwsh coordination.ps1 -Session <sid> -Lesson "..."   # append one lesson line (turn end)
#   pwsh coordination.ps1 -Session <sid> -Patterns        # print the failure-pattern registry (top of lessons.md)
#   pwsh coordination.ps1 -Session <sid> -PatternStats    # count [RECUR] occurrences per pattern tag
#   pwsh coordination.ps1 -Session <sid> -LessonQuery "X" # grep lessons.md for a keyword (case-insensitive)
#
# Exit code 1 on any violation. Run from the repo root. zones.md format: one "owner: prefix" per
# line; "#" comments allowed; owner is the session id without the "sess-" prefix.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Session,
    [string]$TrackRoot,          # relative to repo root; adjust on install
    [switch]$Start,
    [switch]$Heartbeat,
    [switch]$Gate,
    [switch]$LockVerify,
    [switch]$UnlockVerify,
    [switch]$LockCheck,
    [switch]$CloseOut,
    [switch]$Lessons,
    [string]$Lesson,
    [switch]$Patterns,
    [switch]$PatternStats,
    [int]$LessonCount = 10,
    [int]$StaleHours = 12
)

$ErrorActionPreference = 'Stop'
$Session = $Session -replace '^sess-', ''
$root = $PWD.Path

if (-not $TrackRoot) {
    $TrackRoot = 'workflow/track-main'   # edit for your track; e.g. 'workflow/parity'
}
$trackDir = Join-Path $root ($TrackRoot -replace '/', '\')
$liveDir = Join-Path $trackDir 'live'
$locksDir = Join-Path $liveDir 'locks'
$lockFile = Join-Path $locksDir 'verify.lock'
$sessionFile = Join-Path $liveDir "sess-$Session.md"
$lessonsFile = Join-Path $liveDir 'lessons.md'
$summaryDir = Join-Path $trackDir '00_summary'
$statusFile = Join-Path $trackDir 'STATUS.md'
$zonesFile = Join-Path $trackDir 'zones.md'
$failures = @()

# ---------------------------------------------------------------------------
# Zone map — parsed from zones.md (single authority). "owner: prefix" lines.
# ---------------------------------------------------------------------------
$zoneMap = @{}
function Load-ZoneMap {
    if (-not (Test-Path $zonesFile)) {
        $script:zonesMissing = $true
        return
    }
    $script:zonesMissing = $false
    foreach ($line in Get-Content $zonesFile) {
        $t = $line.Trim()
        if (-not $t -or $t.StartsWith('#')) { continue }
        if ($t -match '^([^\s:]+)\s*:\s*(.+)$') {
            $owner = $Matches[1] -replace '^sess-', ''
            $prefix = $Matches[2].Trim().Replace('\', '/')
            if (-not $zoneMap.ContainsKey($owner)) { $zoneMap[$owner] = @() }
            $zoneMap[$owner] += $prefix
        }
    }
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
        $content = [regex]::Replace($content, '(heartbeat:\s*)[0-9]{4}-[0-9]{2}-[0-9]{2}\s+[0-9]{2}:[0-9]{2}', "`${1}$now", 1)
    }
    Set-Content -Path $file -Value $content -Encoding UTF8 -NoNewline
    Write-Host "  heartbeat stamped: $now (in sess-$Session.md)"
}

function Get-LastUpdate([string]$file) {
    if (-not (Test-Path $file)) { return $null }
    $line = Get-Content $file | Where-Object { $_ -match '^Last Update:|\*Last Update:' } | Select-Object -First 1
    if (-not $line) { return $null }
    $m = [regex]::Match($line, '([0-9]{4}-[0-9]{2}-[0-9]{2}(?:\s+[0-9]{2}:[0-9]{2})?)')
    if (-not $m.Success) { return $null }
    if ($m.Groups[1].Value.Length -gt 10) {
        return [datetime]::ParseExact($m.Groups[1].Value.Trim(), 'yyyy-MM-dd HH:mm', $null)
    }
    return [datetime]::ParseExact($m.Groups[1].Value.Trim(), 'yyyy-MM-dd', $null)
}

function Test-OwnsPath([string]$path) {
    $normalized = $path.Replace('\', '/')
    if ($zonesMissing -or -not $zoneMap.ContainsKey($Session)) { return $true } # unknown session: warn, don't block
    foreach ($prefix in $zoneMap[$Session]) {
        $p = $prefix.TrimEnd('/')
        if ($normalized -eq $p -or $normalized.StartsWith("$p/", [StringComparison]::OrdinalIgnoreCase)) { return $true }
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
    $shared = @(
        "$TrackRoot/live/sess-$Session.md",
        "$TrackRoot/live/board.md", "$TrackRoot/live/README.md",
        "$TrackRoot/live/_template.md", "$TrackRoot/live/locks/"
    )
    foreach ($s in $shared) {
        $sn = $s.Replace('\', '/')
        if ($normalized -eq $sn -or $normalized.StartsWith("$sn/", [StringComparison]::OrdinalIgnoreCase)) { return $null }
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

Load-ZoneMap

# --- Patterns / PatternStats / Lessons query: no session file needed ---
if ($Patterns -or $PatternStats) {
    $lessonsPath = Join-Path $liveDir 'lessons.md'
    if (-not (Test-Path $lessonsPath)) {
        Write-Host "ERROR: $lessonsPath not found." -ForegroundColor Red; exit 1
    }
    $content = Get-Content $lessonsPath -Raw
    if ($Patterns) {
        $lines = $content -split "`n"
        $matched = $lines | Where-Object { $_ -match '^#  \[' }
        if ($matched.Count -eq 0) { Write-Host "No patterns found in lessons.md" }
        else { Write-Host "`n=== Failure Pattern Registry ===" -ForegroundColor Cyan; $matched | ForEach-Object { Write-Host $_ } }
        exit 0
    }
    if ($PatternStats) {
        $lines = $content -split "`n"
        $matched = $lines | Where-Object { $_ -match '\[RECUR\]' }
        if ($matched.Count -eq 0) { Write-Host "No [RECUR] tags found in lessons.md" }
        else {
            Write-Host "`n=== Pattern Recurrence Stats ===" -ForegroundColor Cyan
            $matched | ForEach-Object {
                if ($_ -match '\[(\w[\w-]*)\]') { $Matches[1] }
            } | Group-Object | Sort-Object Count -Descending | ForEach-Object {
                Write-Host ("  [{0}] — {1} occurrences" -f $_.Name, $_.Count)
            }
            Write-Host "`nTotal [RECUR] events: $($matched.Count)"
        }
        exit 0
    }
}

if (-not (Test-Path $liveDir)) {
    Write-Host "ERROR: $liveDir does not exist - are you in the repo root, or is -TrackRoot wrong?" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $sessionFile)) {
    Write-Host "ERROR: no session file $liveDir\sess-$Session.md - create one from live/_template.md first." -ForegroundColor Red
    exit 1
}

if ($zonesMissing) {
    Write-Host "WARN: $zonesFile missing - ownership checks are DISABLED. Seed zones.md from the track template." -ForegroundColor Yellow
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
        Write-Host "ERROR: verify.lock already exists - another session is running verify." -ForegroundColor Red
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

if ($Lessons) {
    Write-Host "=== coordination: lessons tail (last $LessonCount) ==="
    if (-not (Test-Path $lessonsFile)) {
        Write-Host "  (no lessons yet - add the first with -Lesson '...' at turn end)"
    }
    else {
        Get-Content $lessonsFile | Select-Object -Last $LessonCount | ForEach-Object { Write-Host "  $_" }
    }
}

if ($Lesson) {
    if (-not (Test-Path $lessonsFile)) {
        "@ lessons.md - session lesson ledger (append-only). `n# Read the tail at every turn start; append exactly one line at every turn end. Each line: <timestamp> sess-<id> :: <failure -> root cause -> remedy -> proof>. The Gate refuses to pass while this file is missing." |
            Set-Content -Path $lessonsFile -Encoding UTF8
    }
    $entry = "- $(Get-Date -Format 'yyyy-MM-dd HH:mm') sess-$Session :: $Lesson"
    Add-Content -Path $lessonsFile -Value $entry -Encoding UTF8
    Write-Host "  lesson appended: $entry"
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

    if (-not (Test-Path $lessonsFile)) {
        $failures += 'live/lessons.md is missing - append a lesson before staging: coordination.ps1 -Session <sid> -Lesson "..."'
    }

    if (Test-Path $lockFile) {
        $lockOwner = Get-Content $lockFile -Raw -ErrorAction SilentlyContinue
        if ($lockOwner -notmatch $Session) {
            $failures += "verify.lock is held by another session ($lockOwner) - do not build/test shared projects"
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
        elseif ($line -match '^(.{2})\s+(.+)$') {
            $path = $Matches[2]
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

if ($CloseOut) {
    Write-Host "=== coordination: wave close-out gate ($Session) ==="

    $mySummaries = @()
    if (Test-Path $summaryDir) {
        $mySummaries = Get-ChildItem $summaryDir -Filter "implementation-summary-*-$Session.md" -File |
            Sort-Object Name -Descending
    }
    if ($mySummaries.Count -eq 0) {
        $myName = "implementation-summary-$(Get-Date -Format 'yyyy-MM-dd-HH-mm-ss')-$Session.md"
        $failures += "no summary for session '$Session' - write 00_summary/$myName per _template.md (with ## Lessons section) before committing"
    }
    else {
        $newestSummary = $mySummaries[0]
        Write-Host "  newest summary: $($newestSummary.Name)"

        $sumText = Get-Content $newestSummary.FullName -Raw -Encoding UTF8
        $heading = [regex]::Match($sumText, '(?im)^##\s+Lessons\s+/')
        if (-not $heading.Success) {
            $failures += "newest summary lacks '## Lessons / Process Improvements' section - add it per _template.md"
        }
        else {
            $sectionSlice = $sumText.Substring($heading.Index)
            $nextHeading = [regex]::Match($sectionSlice, '(?m)^##\s+', 1)
            if ($nextHeading.Success) {
                $sectionSlice = $sectionSlice.Substring(0, $nextHeading.Index)
            }
            [int]$bulletCount = ([regex]::Matches($sectionSlice, '(?m)^\s*-\s+')).Count
            if ($bulletCount -gt 4) {
                $failures += "newest summary has $bulletCount bullets in ## Lessons - max 3 (plus optional '- (none)')"
            }
        }
    }

    $hb = Get-Heartbeat $sessionFile
    $lu = Get-LastUpdate $statusFile
    if ($null -eq $hb) {
        $failures += 'session file has no parseable heartbeat - run -Start first'
    }
    elseif ($null -eq $lu) {
        $failures += "STATUS.md has no parseable 'Last Update' line - refresh it this wave"
    }
    elseif ($lu -lt $hb) {
        $failures += "STATUS.md 'Last Update' ($($lu.ToString('yyyy-MM-dd HH:mm'))) is older than the heartbeat ($($hb.ToString('yyyy-MM-dd HH:mm'))) - refresh STATUS.md this wave"
    }
    else {
        Write-Host "  STATUS.md refreshed ($($lu.ToString('yyyy-MM-dd HH:mm')))."
    }

    if ($failures.Count -gt 0) {
        Write-Host "`nCLOSE-OUT FAILED:" -ForegroundColor Red
        $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
        Write-Host "  Fix the violations, then re-run -CloseOut. Blocking staging until it passes." -ForegroundColor Red
        exit 1
    }
    Write-Host "`nCLOSE-OUT PASSED - summary + Lessons + STATUS.md are current." -ForegroundColor Green
}