# init-saas-workflow.ps1 — One-command SaaS bootstrap from the FullStackHero .NET Starter Kit.
# Clone/copy -> rename (FSH.* -> <Name>.*) -> optional strip -> workflow wiring -> first-track
# seed (opencode/_tracks-template) -> build gate -> first commit -> next-steps. Designed so you
# can focus on the new SaaS's domain, not ceremonies.
#
# Usage:
#   pwsh opencode/init-saas-workflow.ps1 -Name AcmeSaaS -WorkDir C:\dev\AcmeSaaS
#     [-FromClone <path-to-this-repo>]          # default: the repo containing this script
#     [-StripReact] [-StripMaui]                # drop React clients / MAUI Hybrid app
#     [-StripModules Catalog,Billing]           # drop backend modules (comma list)
#     [-SkipClone]                              # source already scaffolded (fsh new --workflow)
#     [-SkipBuild] [-NoCommit]                  # skip build gate / skip first commit
#     [-NonInteractive]                         # accept defaults, no prompts
#
# Exit codes: 0 = ready for dev · 1 = validation error · 2 = step failure (see output)

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Name,
    [string]$WorkDir,
    [string]$FromClone,
    [switch]$StripReact,
    [switch]$StripMaui,
    [string]$StripModules,
    [switch]$SkipClone,
    [switch]$SkipBuild,
    [switch]$NoCommit,
    [switch]$NonInteractive
)

$ErrorActionPreference = 'Stop'
$started = Get-Date

function Write-Step([string]$title) { Write-Host "`n=== $title ===" -ForegroundColor Cyan }
function Write-Ok([string]$msg) { Write-Host "  OK - $msg" -ForegroundColor Green }
function Write-Warn([string]$msg) { Write-Host "  WARN - $msg" -ForegroundColor Yellow }
function Confirm-Step([string]$prompt) {
    if ($NonInteractive) { return $true }
    $a = Read-Host "$prompt (y/n)"
    return $a -match '^[yY]'
}

# Workflow files kept byte-identical (they are the origin tooling, not the product).
# Only the SCRIPT stays pristine (its own rename map must not self-corrupt); the
# AGENTIC-GUIDE.md is a doc that ships INTO the new repo, so it IS renamed like any
# other content file.
$workflowKeep = @('opencode\init-saas-workflow.ps1')

# ---------------------------------------------------------------------------
# 0. Validation
# ---------------------------------------------------------------------------
if ($Name -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
    Write-Host "ERROR: -Name '$Name' is not a valid C# identifier (use e.g. AcmeSaaS)." -ForegroundColor Red
    exit 1
}
$scriptRoot = (Resolve-Path $PSScriptRoot).Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..')).Path
if (-not $WorkDir) { $WorkDir = Join-Path (Get-Location) $Name }
$WorkDir = [System.IO.Path]::GetFullPath($WorkDir)
if (-not $FromClone) { $FromClone = $repoRoot }
$FromClone = [System.IO.Path]::GetFullPath($FromClone)

Write-Host "FullStackHero -> SaaS bootstrap: '$Name' -> $WorkDir" -ForegroundColor Cyan
Write-Host "  source: $FromClone`n"

if (Test-Path $WorkDir) {
    if ((Get-ChildItem -Force $WorkDir | Measure-Object).Count -gt 0) {
        if (-not (Confirm-Step "WorkDir '$WorkDir' exists and is not empty. Continue into it?")) {
            Write-Host "Aborted."; exit 1
        }
    }
} else {
    New-Item -ItemType Directory -Path $WorkDir -Force | Out-Null
}

# ---------------------------------------------------------------------------
# 1. Clone / copy (skip if source already scaffolded)
# ---------------------------------------------------------------------------
if (-not $SkipClone) {
    Write-Step "1/7 Copying source (excludes .git, bin/obj, node_modules, .swarm)"
    $excludeDirs = @('.git', '.swarm', 'bin', 'obj', 'node_modules', 'dist', 'build',
                     'test-results', 'playwright-report', '.turbo', 'coverage', '.vs', '.idea', '.vscode')
    $excludeFiles = @('*.user', '*.lock.json', '.env', '.env.*', 'global.json')
    $roboArgs = @($FromClone, $WorkDir, '/E', '/NFL', '/NDL', '/NJH', '/NJS', '/NC', '/NS', '/NP')
    foreach ($d in $excludeDirs) { $roboArgs += '/XD'; $roboArgs += $d }
    foreach ($f in $excludeFiles) { $roboArgs += '/XF'; $roboArgs += $f }
    robocopy @roboArgs | Out-Null
    if ($LASTEXITCODE -ge 8) { Write-Host "ERROR: robocopy failed ($LASTEXITCODE)." -ForegroundColor Red; exit 2 }
    $LASTEXITCODE = 0
    Write-Ok "copied $FromClone -> $WorkDir"
} else {
    Write-Step "1/7 Skipping clone (already scaffolded)"
}

Set-Location $WorkDir
if (-not (Test-Path 'src')) { Write-Host "ERROR: no 'src' directory in $WorkDir - is this an FSH scaffold?" -ForegroundColor Red; exit 1 }

# ---------------------------------------------------------------------------
# 2. Rename ritual: FSH.Starter -> <Name> | FSH -> <Name> | FullStackHero -> <Name>
# ---------------------------------------------------------------------------
Write-Step "2/7 Renaming FSH.* -> $Name.* (files, folders, contents)"

# Replacement map — longest keys MUST run first ('FSH.Starter' before 'FSH')
$contentReplacements = [ordered]@{
    'FSH.Starter'   = $Name
    'FSH_Starter'   = $Name
    'FullStackHero' = $Name
    'FSH'           = $Name
}
$sortedKeys = @($contentReplacements.Keys | Sort-Object { $_.Length } -Descending)

$isWorkflowKeep = { param([string]$p)
    foreach ($k in $workflowKeep) { if ($p -like "*$k") { return $true } }
    return $false
}

# 2a. Rename files (deepest-first so nested paths stay valid)
$renamed = 0
$allFiles = Get-ChildItem -LiteralPath $WorkDir -Recurse -Force -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules|\.git)\\' } |
    Sort-Object { $_.FullName.Length } -Descending
foreach ($f in $allFiles) {
    if (& $isWorkflowKeep $f.FullName) { continue }
    $newName = $f.Name
    foreach ($t in $sortedKeys) { $newName = $newName.Replace($t, $contentReplacements[$t]) }
    if ($newName -ne $f.Name) {
        Move-Item -LiteralPath $f.FullName -Destination (Join-Path $f.DirectoryName $newName)
        $renamed++
    }
}
# 2b. Rename folders (deepest-first, try/catch for stale entries after parent moves)
for ($i = 0; $i -lt 5; $i++) {
    $moved = $false
    $dirs = Get-ChildItem -LiteralPath $WorkDir -Recurse -Directory -Force |
        Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules|\.git)\\' } |
        Sort-Object { $_.FullName.Length } -Descending
    foreach ($d in $dirs) {
        $newName = $d.Name
        foreach ($t in $sortedKeys) { $newName = $newName.Replace($t, $contentReplacements[$t]) }
        if ($newName -ne $d.Name) {
            try {
                Move-Item -LiteralPath $d.FullName -Destination (Join-Path $d.Parent.FullName $newName)
                $renamed++; $moved = $true
            } catch { }  # stale entry (parent already moved this pass) — next pass handles it
        }
    }
    if (-not $moved) { break }
}
Write-Ok "renamed $renamed files/folders"

# 2c. Replace in file contents (binary-safe null-byte probe; skip workflow keep + git internals)
$changedFiles = 0
foreach ($f in (Get-ChildItem -LiteralPath $WorkDir -Recurse -Force -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules|\.git)\\' -and -not (& $isWorkflowKeep $_.FullName) })) {
    $isText = $true
    $fs = [System.IO.File]::OpenRead($f.FullName)
    try {
        $buf = New-Object byte[] 512
        $n = $fs.Read($buf, 0, 512)
        for ($i = 0; $i -lt $n; $i++) { if ($buf[$i] -eq 0) { $isText = $false; break } }
    } finally { $fs.Dispose() }
    if (-not $isText) { continue }
    $content = [System.IO.File]::ReadAllText($f.FullName)
    $changed = $false
    foreach ($t in $sortedKeys) {
        if ($content.Contains($t)) { $content = $content.Replace($t, $contentReplacements[$t]); $changed = $true }
    }
    if ($changed) { [System.IO.File]::WriteAllText($f.FullName, $content); $changedFiles++ }
}
Write-Ok "rewrote content in $changedFiles files"

# 2d. Verification sweep — residual FSH references (capital F only; workflow files exempt)
Write-Step "2/7b Residual reference sweep"
$residual = @()
foreach ($f in (Get-ChildItem -LiteralPath $WorkDir -Recurse -Force -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules|\.git)\\' -and -not (& $isWorkflowKeep $_.FullName) })) {
    try { $text = [System.IO.File]::ReadAllText($f.FullName) } catch { continue }
    foreach ($m in [regex]::Matches($text, '(?<![\w.-])FSH(?![\w-])')) {
        $lineStart = $text.LastIndexOf("`n", [Math]::Min($m.Index, $text.Length)) + 1
        $line = $text.Substring($lineStart, [Math]::Min(80, $text.Length - $lineStart)).Trim()
        $residual += "$($f.FullName.Substring($WorkDir.Length + 1)): $line"
    }
}
if ($residual.Count -eq 0) {
    Write-Ok "no residual FSH references"
} else {
    Write-Warn "$($residual.Count) residual FSH references (review + fix manually):"
    $residual | Select-Object -Unique | ForEach-Object { Write-Host "    $_" -ForegroundColor Yellow }
}

# ---------------------------------------------------------------------------
# 3. Strip options
# ---------------------------------------------------------------------------
Write-Step "3/7 Applying strip options"
if ($StripReact) {
    foreach ($p in @('clients\admin', 'clients\dashboard')) {
        if (Test-Path $p) { Remove-Item -Recurse -Force $p; Write-Ok "removed $p" }
    }
}
if ($StripMaui) {
    if (Test-Path 'clients\FSH.Hybrid') { Remove-Item -Recurse -Force 'clients\FSH.Hybrid'; Write-Ok "removed clients/FSH.Hybrid" }
}
if ($StripModules) {
    foreach ($m in ($StripModules -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })) {
        $folder = "src\Modules\$m"
        if (Test-Path $folder) {
            Remove-Item -Recurse -Force $folder
            Write-Ok "removed module $m"

            # 1. slnx: module folder + project entries + test project entries
            $slnx = Get-ChildItem 'src' -Filter '*.slnx' | Select-Object -First 1
            if ($slnx) {
                $xml = [System.IO.File]::ReadAllText($slnx.FullName)
                $xml = $xml -replace "(?s)<Folder Name=`"/Modules/$m/`">.*?</Folder>", ''
                $xml = $xml -replace "(?m)^\s*<Project Path=`"Modules/$m/[^`"]*`"[^>]*/>\s*$", ''
                $xml = $xml -replace "(?m)^\s*<Project Path=`"Tests/$m\.Tests/[^`"]*`"[^>]*/>\s*$", ''
                [System.IO.File]::WriteAllText($slnx.FullName, $xml)
            }

            # 2. migration folder in the Migrations project
            $migrations = Get-ChildItem 'src\Host' -Directory -Filter '*Migrations*' | Select-Object -First 1
            if ($migrations) {
                $migFolder = Join-Path $migrations.FullName $m
                if (Test-Path $migFolder) { Remove-Item -Recurse -Force $migFolder; Write-Ok "removed migration folder $migFolder" }
            }

            # 3. every csproj under src/: drop ProjectReference lines whose path contains the module dir segment
            #    + dangling <Folder Include="<m>\" /> items (e.g. Migrations' per-module migration folders)
            $projRefPattern = "(?m)^\s*<ProjectReference Include=`"[^\`"]*(?<![A-Za-z0-9])$m[\\/][^\`"]*`"[^>]*/>\s*$"
            $folderIncludePattern = "(?m)^\s*<Folder Include=`"$m\\`"\s*/>\s*$"
            Get-ChildItem 'src' -Recurse -Filter '*.csproj' | ForEach-Object {
                $content = [System.IO.File]::ReadAllText($_.FullName)
                if ($content -match $projRefPattern -or $content -match $folderIncludePattern) {
                    $content = $content -replace $projRefPattern, ''
                    $content = $content -replace $folderIncludePattern, ''
                    [System.IO.File]::WriteAllText($_.FullName, $content)
                    Write-Ok "removed $m reference from $($_.FullName)"
                }
            }

            # 4. cross-module consumers: .cs files in other modules/hosts/tests importing the module
            #    namespace. Tier rules:
            #    - IntegrationEventHandlers\ paths -> delete (pure cross-module event subscribers)
            #    - DemoSeed\ (shared orchestrator seeder) -> strip using lines + the module's region
            #      block + SeedTenant<m> call sites
            #    - files importing OTHER modules too (mixed, e.g. ContractsPurityTests) -> strip
            #      using lines + typeof(...Modules.<m>...) lines; build gate catches the rest
            #    - otherwise (pure consumer) -> delete
            $name = (Get-ChildItem 'src' -Filter '*.slnx' | Select-Object -First 1).BaseName
            $usingPattern = "(?m)^\s*using $name\.Modules\.$m(?:\.|;).*$"
            $otherModulePattern = "(?m)^\s*using $name\.Modules\.(?!$m(?:\.|;)).*$"
            Get-ChildItem 'src\Modules','src\Host','src\Tests' -Recurse -Filter '*.cs' -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch "\\Modules\\$m\\" } | ForEach-Object {
                $content = [System.IO.File]::ReadAllText($_.FullName)
                if ($_.FullName -match "\\Tests\\$m\\") {
                    Remove-Item -Force $_.FullName
                    Write-Warn "removed module-scoped test file: $($_.FullName)"
                } elseif ($content -match $usingPattern) {
                    if ($_.FullName -match '\\IntegrationEventHandlers\\') {
                        Remove-Item -Force $_.FullName
                        Write-Warn "removed cross-module event subscriber: $($_.FullName)"
                    } elseif ($_.FullName -match '\\DemoSeed\\') {
                        $updated = $content -replace $usingPattern, ''
                        $updated = $updated -replace "(?ms)^\s*//\s*─+\s*$m\s*─+.*?(?=^\s*//\s*─|\z)", ''
                        $updated = $updated -replace "(?m)^\s*await SeedTenant$m\w*\(.*$", ''
                        if ($updated -ne $content) {
                            [System.IO.File]::WriteAllText($_.FullName, $updated)
                            Write-Warn "kept shared seeder, stripped $m region: $($_.FullName)"
                        }
                    } elseif ($content -match $otherModulePattern) {
                        $updated = $content -replace $usingPattern, ''
                        $updated = $updated -replace "(?m)^\s*typeof\($name\.Modules\.$m\..*\),?$", ''
                        $updated = $updated -replace "(?m)^\s*typeof\($m\w*ContractsMarker\)[^\r\n]*$", ''
                        if ($updated -ne $content) {
                            [System.IO.File]::WriteAllText($_.FullName, $updated)
                            Write-Warn "kept mixed file, stripped $m lines: $($_.FullName)"
                        }
                    } else {
                        Remove-Item -Force $_.FullName
                        Write-Warn "removed cross-module consumer: $($_.FullName)"
                    }
                }
            }

            # 5. XML doc crefs pointing at the stripped module in ANY remaining .cs file
            $crefPattern = "(?s)<see cref=`"[^\`"]*\b$m\.[^\`"]*`"\s*/>"
            Get-ChildItem 'src\Modules','src\Host','src\Tests' -Recurse -Filter '*.cs' -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch "\\Modules\\$m\\" } | ForEach-Object {
                $content = [System.IO.File]::ReadAllText($_.FullName)
                if ($content -match $crefPattern) {
                    $updated = $content -replace $crefPattern, ''
                    [System.IO.File]::WriteAllText($_.FullName, $updated)
                    Write-Warn "stripped $m crefs from $($_.Name)"
                }
            }

            # 6. the FOUR registration sites (Api + DbMigrator Program.cs): typeof(...) + using lines
            foreach ($prog in @('src\Host\*\Program.cs')) {
                Get-ChildItem $prog -ErrorAction SilentlyContinue | ForEach-Object {
                    $content = [System.IO.File]::ReadAllText($_.FullName)
                    $updated = $content -replace "(?m)^\s*using $name\.Modules\.$m;.*$", '' -replace "(?m)^\s*typeof\($name\.Modules\.$m\..*$,?$", ''
                    if ($updated -ne $content) {
                        [System.IO.File]::WriteAllText($_.FullName, $updated)
                        Write-Ok "removed $m registration lines from $($_.Name)"
                    }
                }
            }

            # 7. per-module test project
            if (Test-Path "src\Tests\$m.Tests") { Remove-Item -Recurse -Force "src\Tests\$m.Tests"; Write-Ok "removed test project $m.Tests" }
            Write-Warn "module $m stripped - review the removed cross-module consumers above; the build gate is the final check"
        } else {
            Write-Warn "module '$m' not found - skipped"
        }
    }
}
if (-not $StripReact -and -not $StripMaui -and -not $StripModules) { Write-Ok "nothing stripped (default: keep everything)" }

# ---------------------------------------------------------------------------
# 4. Clean project-private dirs + session state (workflow tooling kept)
# ---------------------------------------------------------------------------
Write-Step "4/7 Cleaning project-private directories"
foreach ($p in @('opencode\addBlazorFrontends', 'opencode\Next apps', 'opencode\other', 'opencode\temp', '.swarm', 'superpowers', 'templates')) {
    if (Test-Path $p) { Remove-Item -Recurse -Force $p; Write-Ok "removed $p" }
}
Write-Ok "kept: opencode/init-saas-workflow.ps1, opencode/AGENTIC-GUIDE.md, .opencode/, .agents/"

# ---------------------------------------------------------------------------
# 5. Wire the agentic workflow
# ---------------------------------------------------------------------------
Write-Step "5/7 Wiring agentic workflow"
New-Item -ItemType Directory -Path '.opencode' -Force | Out-Null
if (-not (Test-Path '.opencode\opencode-swarm.json')) {
    Set-Content -Path '.opencode\opencode-swarm.json' -Value '{}' -Encoding UTF8
    Write-Ok "created .opencode/opencode-swarm.json (empty - extends global swarm config)"
}
if (-not (Test-Path '.opencode\skill-routing.yaml')) {
    @'
# Skill routing - FSH skills are authoritative for structure; global dotnet-skills inform technique.
# FSH wins on structure, dotnet-skill informs technique (see opencode/AGENTIC-GUIDE.md).
routing:
  backend-feature: add-feature, query-patterns, mediator-reference
  entity-migration: add-entity, create-migration
  permission: add-permission
  blazor-page: add-blazor-page, implement-blazor-list, implement-blazor-form
  blazor-auth: setup-blazor-auth, setup-blazor-realtime, setup-blazor-sse
  react-page: add-react-page
  module: add-module
  testing: testing-guide
'@ | Set-Content -Path '.opencode\skill-routing.yaml' -Encoding UTF8
    Write-Ok "created .opencode/skill-routing.yaml"
}
$gi = if (Test-Path '.gitignore') { Get-Content '.gitignore' -Raw } else { '' }
if ($gi -notmatch '(?m)^\.swarm/?$') {
    Add-Content -Path '.gitignore' -Value "`n# Agentic workflow state`n.swarm/`nopencode/temp/`nopencode/live/" -Encoding UTF8
    Write-Ok "appended agentic entries to .gitignore"
}
Write-Ok "AGENTS.md + .agents/ carried over from source (rename pass already rewrote references)"

# ---------------------------------------------------------------------------
# 5b. Seed the first track from _tracks-template
# ---------------------------------------------------------------------------
Write-Step "5b/7 Seeding first track (opencode/_tracks-template -> opencode/$Name)"
$tracksTemplate = Join-Path $scriptRoot '_tracks-template'
$trackDir = Join-Path $WorkDir "opencode\$Name"
if (Test-Path $tracksTemplate) {
    if (Test-Path $trackDir) {
        Write-Warn "opencode/$Name already exists - track seed skipped"
    } else {
        New-Item -ItemType Directory -Path $trackDir -Force | Out-Null
        Copy-Item -Path (Join-Path $tracksTemplate '*') -Destination $trackDir -Recurse -Force
        Get-ChildItem $trackDir -Filter '*-template.md' -File | ForEach-Object {
            $realName = $_.Name -replace '-template\.md$', '.md'
            Move-Item -LiteralPath $_.FullName -Destination (Join-Path $trackDir $realName) -Force
            Write-Ok "renamed $($_.Name) -> $realName"
        }
        $today = (Get-Date).ToString('yyyy-MM-dd')
        Get-ChildItem $trackDir -Recurse -Filter '*.md' -File | ForEach-Object {
            $content = Get-Content $_.FullName -Raw
            $updated = $content -replace '<track-name>', $Name -replace '\{date\}', $today
            if ($updated -ne $content) {
                Set-Content -Path $_.FullName -Value $updated -Encoding UTF8
                Write-Ok "filled placeholders in $($_.Name)"
            }
        }
        New-Item -ItemType Directory -Path (Join-Path $trackDir 'live\locks') -Force | Out-Null
        Write-Ok "first track seeded: opencode/$Name/ (00-Index.md, STATUS.md, readme.md, zones.md, plan.md, live/, 00_summary/)"
    }
} else {
    Write-Warn "opencode/_tracks-template not found - first track must be created manually"
}

# ---------------------------------------------------------------------------
# 6. Restore + build gate
# ---------------------------------------------------------------------------
if (-not $SkipBuild) {
    Write-Step "6/7 Restore + build gate"
    $slnx = Get-ChildItem 'src' -Filter '*.slnx' | Select-Object -First 1
    if (-not $slnx) {
        Write-Warn "no .slnx found under src/ - build gate skipped"
    } else {
        Write-Host "  restoring: $($slnx.Name)"
        dotnet restore $slnx.FullName 2>&1 | ForEach-Object { Write-Host "    $_" }
        if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: restore failed." -ForegroundColor Red; exit 2 }
        Write-Host "  building: $($slnx.Name)"
        $build = dotnet build $slnx.FullName 2>&1
        $errs = @($build | Where-Object { $_ -match ': error ' }).Count
        $warns = @($build | Where-Object { $_ -match ': warning ' }).Count
        Write-Host "  build: $errs errors / $warns warnings"
        if ($errs -gt 0) {
            Write-Host "ERROR: build failed - fix these first (residual rename or strip fallout):" -ForegroundColor Red
            $build | Where-Object { $_ -match ': error ' } | Select-Object -First 20 | ForEach-Object { Write-Host "    $_" }
            exit 2
        }
        if ($warns -gt 0) { Write-Warn "build has $warns warnings - repo convention is 0 warnings" }
        else { Write-Ok "build 0 errors / 0 warnings" }
    }
} else {
    Write-Step "6/7 Skipping build gate (-SkipBuild)"
}

# ---------------------------------------------------------------------------
# 7. First commit + next steps
# ---------------------------------------------------------------------------
if (-not $NoCommit) {
    Write-Step "7/7 First commit"
    git init -q 2>$null
    git add -A
    git -c user.name="init-saas-workflow" -c user.email="noreply@local" commit -q -m "chore: bootstrap $Name from FullStackHero .NET Starter Kit"
    if ($LASTEXITCODE -ne 0) {
        Write-Warn "git commit failed (check git config user.name/email) - run 'git add -A && git commit' manually"
    } else { Write-Ok "initial commit created" }
} else {
    Write-Step "7/7 Skipping first commit (-NoCommit)"
}

# ---------------------------------------------------------------------------
# Done
# ---------------------------------------------------------------------------
$elapsed = ((Get-Date) - $started).TotalSeconds
Write-Host "`n=== BOOTSTRAP COMPLETE ($([math]::Round($elapsed))s) ===" -ForegroundColor Green
Write-Host @"

  $Name is ready at: $WorkDir

  Next steps (copy-paste):
    cd $WorkDir
    # 1. Review the residual-reference report above (if any) and the strip warnings (module
    #    registration sites). Build is the gate: dotnet build src/$Name.slnx
    # 2. Open your first agentic session:
    #    opencode   # then: "Read AGENTS.md + opencode/AGENTIC-GUIDE.md, then scaffold the
    #              #  domain module for <your domain> using the add-module skill."
    #    Track skeleton ready at opencode/$Name/ (00-Index.md, STATUS.md, readme.md, zones.md,
    #    live/, 00_summary/) - read those first; fill zones.md + STATUS.md before claiming work.
    # 3. Run the stack: dotnet run --project src/Host/$Name.AppHost
    # 4. Future re-runs: pwsh opencode/init-saas-workflow.ps1 -Name $Name -WorkDir $WorkDir -SkipClone -SkipBuild

  Kept for you:
    .agents/skills/  (add-module, add-feature, add-blazor-page, create-migration, ...)
    .agents/workflows/ + .opencode/  (Swarm + skill routing)
    opencode/init-saas-workflow.ps1 + opencode/AGENTIC-GUIDE.md  (origin tooling - reusable)
    opencode/_tracks-template/ + opencode/$Name/  (track harness + first seeded track)
    AGENTS.md (renamed, still the canonical guide)
"@
exit 0
