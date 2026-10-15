<#
.SYNOPSIS
  Reproducible, reversible Development Host lifecycle for the Tooba repository.

.DESCRIPTION
  Provides a single deterministic operator procedure to build, start, inspect and stop the
  Development Tooba Host on an explicit loopback port. It exists because the shared
  Development Host used to be started by hand, leaving an orphaned process with no recorded
  PID file, no reproducible stop and no verifiable rollback.

  Guarantees:
    * deterministic project path, output directory, working directory and content root;
    * explicit ASPNETCORE_ENVIRONMENT=Development and an explicit loopback URL;
    * a PID file plus stdout/stderr logs under .tmp-dev-host/ (git-ignored, never staged);
    * /health and /__platform-commerce verification before the run is reported healthy;
    * refusal to stop or start over a port owned by an unrelated process;
    * graceful stop by default (no Stop-Process -Force unless -Force is explicitly passed);
    * no credential, token or connection string is written to any file or log.

  The one-level Shipping catalog gate is enabled only through an ephemeral process
  environment variable when -GateShippingOneLevelCatalog is passed. No appsettings file is
  created or modified, so the gate always falls back to its disabled default.

.EXAMPLE
  pwsh -File scripts/dev/host-stack.ps1 -Action status

.EXAMPLE
  pwsh -File scripts/dev/host-stack.ps1 -Action start -GateShippingOneLevelCatalog

.EXAMPLE
  pwsh -File scripts/dev/host-stack.ps1 -Action stop
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('start', 'stop', 'restart', 'status')]
    [string]$Action,

    [int]$Port = 5088,

    [string]$OutDir = '',

    [switch]$GateShippingOneLevelCatalog,

    [switch]$NoBuild,

    # Deliberate, logged takeover of a PRE-EXISTING Development Host on the same port that this
    # script did not start. Allowed only when the running process is positively proven to be this
    # repository's Development Host (name, executable path under this repo, and the exact
    # Development provenance tuple). The prior process facts are written to a record file first,
    # which is what makes the takeover reversible.
    [switch]$TakeOverProvenDevelopmentHost,

    [switch]$Force
)

$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$ProjectPath = Join-Path $RepoRoot 'src\backend\Host\Tooba.Host\Tooba.Host.csproj'
$ContentRoot = Join-Path $RepoRoot 'src\backend\Host\Tooba.Host'
if ([string]::IsNullOrWhiteSpace($OutDir)) {
    $OutDir = Join-Path $RepoRoot ".tmp-dev-host-out-$Port"
}
elseif (-not [System.IO.Path]::IsPathRooted($OutDir)) {
    # Normalise a relative OutDir against the repository root so the recorded executable path is
    # always absolute and the ownership proof on stop can compare it exactly.
    $OutDir = Join-Path $RepoRoot $OutDir
}
$OutDir = [System.IO.Path]::GetFullPath($OutDir)
$StateDir = Join-Path $RepoRoot '.tmp-dev-host'
$PidFile = Join-Path $StateDir "host-$Port.pid"
$OutLog = Join-Path $StateDir "host-$Port-stdout.txt"
$ErrLog = Join-Path $StateDir "host-$Port-stderr.txt"
$BaseUrl = "http://127.0.0.1:$Port"

function Write-Step([string]$Message) { Write-Host "[host-stack] $Message" }

function Get-PortOwner([int]$CandidatePort) {
    Get-NetTCPConnection -State Listen -LocalPort $CandidatePort -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique
}

function Get-ProcessInfo([int]$ProcessId) {
    if ($ProcessId -le 0) { return $null }
    Get-CimInstance Win32_Process -Filter "ProcessId = $ProcessId" -ErrorAction SilentlyContinue
}

function Read-State {
    if (-not (Test-Path $PidFile)) { return $null }
    try { return (Get-Content $PidFile -Raw | ConvertFrom-Json) } catch { return $null }
}

function Write-State($State) {
    if (-not (Test-Path $StateDir)) { New-Item -ItemType Directory -Force -Path $StateDir | Out-Null }
    $State | ConvertTo-Json -Depth 4 | Set-Content -Path $PidFile -Encoding UTF8
}

function Test-Provenance {
    try {
        $response = Invoke-WebRequest -Uri "$BaseUrl/__platform-commerce" -UseBasicParsing -TimeoutSec 10
        if ($response.StatusCode -ne 200) { return $null }
        return ($response.Content | ConvertFrom-Json)
    } catch { return $null }
}

function Assert-DevelopmentProvenance($Provenance) {
    if ($null -eq $Provenance) { throw "Provenance check failed: $BaseUrl/__platform-commerce did not answer 200." }
    $expected = @{
        edition             = 'SingleStore'
        deploymentId        = 'dev-singlestore'
        tenantId            = 'store-alpha'
        connectionReference = 'tenant-alpha'
    }
    foreach ($key in $expected.Keys) {
        $actual = [string]$Provenance.$key
        if ($actual -ne $expected[$key]) {
            throw "Provenance mismatch on $key : expected '$($expected[$key])', observed '$actual'. Refusing to continue."
        }
    }
    Write-Step "provenance OK: $($Provenance.edition) / $($Provenance.deploymentId) / $($Provenance.tenantId) / $($Provenance.connectionReference)"
}

function Wait-ForHealth([int]$TimeoutSeconds = 150) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        try {
            $response = Invoke-WebRequest -Uri "$BaseUrl/health" -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -eq 200) { return $response.Content }
        } catch { }
    }
    return $null
}

function Stop-ManagedHost([switch]$AllowForce) {
    $state = Read-State
    if ($null -eq $state) {
        Write-Step "no PID file at $PidFile - nothing recorded to stop. Use -Action status to inspect port $Port."
        return $false
    }
    $recordedPid = [int]$state.pid
    $live = Get-ProcessInfo $recordedPid
    if ($null -eq $live) {
        Write-Step "recorded PID $recordedPid is not running; clearing stale PID file."
        Remove-Item $PidFile -Force -ErrorAction SilentlyContinue
        return $false
    }

    # Ownership must be proven: same executable path AND the port must actually be owned by it.
    $recordedExe = [string]$state.executablePath
    if (-not [string]::IsNullOrWhiteSpace($recordedExe)) {
        $resolvedRecorded = [System.IO.Path]::GetFullPath($recordedExe)
        $resolvedLive = if ($live.ExecutablePath) { [System.IO.Path]::GetFullPath([string]$live.ExecutablePath) } else { '' }
        if ($resolvedLive -and ($resolvedRecorded -ne $resolvedLive)) {
            throw "PID $recordedPid executable is '$resolvedLive' but PID file recorded '$resolvedRecorded'. Refusing to stop an unverified process."
        }
    }
    $owners = @(Get-PortOwner $Port)
    if ($owners -notcontains $recordedPid) {
        throw "PID $recordedPid does not own port $Port (owners: $($owners -join ', ')). Refusing to stop an unverified process."
    }

    $provenance = Test-Provenance
    if ($null -ne $provenance) {
        Assert-DevelopmentProvenance $provenance
    } else {
        Write-Step "warning: provenance endpoint did not answer; continuing with recorded ownership proof only."
    }

    Write-Step "stopping managed Host pid=$recordedPid exe=$recordedExe (graceful)"
    Stop-Process -Id $recordedPid -ErrorAction Stop

    $deadline = (Get-Date).AddSeconds(20)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 1
        if ($null -eq (Get-ProcessInfo $recordedPid)) { break }
    }

    if ($null -ne (Get-ProcessInfo $recordedPid)) {
        if (-not $AllowForce) {
            throw "PID $recordedPid did not exit within 20s. Not forcing. Re-run with -Force only after confirming this process is safe to terminate."
        }
        Write-Step "forcing termination of pid=$recordedPid (-Force explicitly requested)"
        Stop-Process -Id $recordedPid -Force
        Start-Sleep -Seconds 3
    }

    Remove-Item $PidFile -Force -ErrorAction SilentlyContinue
    Write-Step "stopped; port $Port listeners = $((@(Get-PortOwner $Port)).Count)"
    return $true
}

function Show-Status {
    Write-Step "repo      : $RepoRoot"
    Write-Step "port      : $Port  ($BaseUrl)"
    Write-Step "pid file  : $PidFile"
    $owners = @(Get-PortOwner $Port)
    if ($owners.Count -eq 0) {
        Write-Step "listeners : none"
    } else {
        foreach ($ownerPid in $owners) {
            $info = Get-ProcessInfo $ownerPid
            Write-Step "listener  : pid=$ownerPid name=$($info.Name) exe=$($info.ExecutablePath) start=$($info.CreationDate)"
        }
    }
    $state = Read-State
    if ($null -eq $state) { Write-Step "managed   : no PID file (this process was not started by this script)" }
    else { Write-Step "managed   : pid=$($state.pid) gate=$($state.gate) outDir=$($state.outDir) startedAt=$($state.startedAt)" }

    $provenance = Test-Provenance
    if ($null -ne $provenance) {
        Write-Step "provenance: $($provenance.edition) / $($provenance.deploymentId) / $($provenance.tenantId) / $($provenance.connectionReference)"
    } else {
        Write-Step "provenance: unavailable"
    }
    try {
        $health = Invoke-WebRequest -Uri "$BaseUrl/health" -UseBasicParsing -TimeoutSec 5
        Write-Step "health    : $($health.StatusCode) $($health.Content)"
    } catch { Write-Step "health    : unavailable" }
}

function Stop-ProvenDevelopmentHost {
    $owners = @(Get-PortOwner $Port)
    if ($owners.Count -eq 0) { Write-Step "no listener on port $Port"; return $false }

    $record = New-Object System.Collections.Generic.List[string]
    $record.Add("TB-093 deliberate takeover of a pre-existing Development Host on port $Port")
    $record.Add("recordedAt=$((Get-Date).ToString('o'))")

    foreach ($ownerPid in $owners) {
        $info = Get-ProcessInfo $ownerPid
        if ($null -eq $info) { throw "Port $Port owner pid=$ownerPid could not be inspected. Refusing." }
        if ($info.Name -ne 'Tooba.Host.exe') {
            throw "Port $Port is owned by '$($info.Name)' (pid=$ownerPid), not Tooba.Host.exe. Refusing to take over an unrelated process."
        }
        $exePath = [string]$info.ExecutablePath
        if ([string]::IsNullOrWhiteSpace($exePath) -or -not $exePath.StartsWith($RepoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Port $Port owner pid=$ownerPid executable '$exePath' is not under this repository ($RepoRoot). Refusing."
        }
        $record.Add("priorPid=$ownerPid")
        $record.Add("priorExecutablePath=$exePath")
        $record.Add("priorCommandLine=$($info.CommandLine)")
        $record.Add("priorParentProcessId=$($info.ParentProcessId)")
        $record.Add("priorCreationDate=$($info.CreationDate)")
        $record.Add("priorParentAlive=$([bool](Get-ProcessInfo ([int]$info.ParentProcessId)))")
    }

    $provenance = Test-Provenance
    Assert-DevelopmentProvenance $provenance
    $record.Add("priorProvenance=$($provenance | ConvertTo-Json -Compress)")
    $record.Add("rollback=stop the process this script starts, then re-run: `"$exePath`"  (or the recorded launch command) to restore the prior state")

    if (-not (Test-Path $StateDir)) { New-Item -ItemType Directory -Force -Path $StateDir | Out-Null }
    $recordFile = Join-Path $StateDir "prior-host-$Port-$((Get-Date).ToString('yyyyMMdd-HHmmss')).txt"
    $record | Set-Content -Path $recordFile -Encoding UTF8
    Write-Step "prior-process record written: $recordFile"

    foreach ($ownerPid in $owners) {
        Write-Step "graceful stop of proven Development Host pid=$ownerPid"
        Stop-Process -Id $ownerPid -ErrorAction Stop
    }
    $deadline = (Get-Date).AddSeconds(25)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 1
        if ((@(Get-PortOwner $Port)).Count -eq 0) { break }
    }
    if ((@(Get-PortOwner $Port)).Count -gt 0) {
        if (-not $Force) { throw "Port $Port still owned after graceful stop. Not forcing without -Force." }
        Write-Step "forcing remaining owners of port $Port (-Force explicitly requested)"
        foreach ($ownerPid in @(Get-PortOwner $Port)) { Stop-Process -Id $ownerPid -Force }
        Start-Sleep -Seconds 3
    }
    Write-Step "port $Port free; prior-process record at $recordFile"
    return $true
}

function Start-ManagedHost {
    $existingOwners = @(Get-PortOwner $Port)
    if ($existingOwners.Count -gt 0) {
        $state = Read-State
        if ($null -ne $state -and $existingOwners -contains [int]$state.pid) {
            Write-Step "already running under this script (pid=$($state.pid)); nothing to do."
            Show-Status
            return
        }
        if (-not $TakeOverProvenDevelopmentHost) {
            throw "Port $Port is owned by process(es) $($existingOwners -join ', ') that this script did not start. Refusing. Re-run with -TakeOverProvenDevelopmentHost only if you intend a deliberate, recorded takeover of this repository's Development Host."
        }
        [void](Stop-ProvenDevelopmentHost)
    }

    if (-not $NoBuild) {
        Write-Step "building $ProjectPath -> $OutDir"
        & dotnet build $ProjectPath -c Debug -o $OutDir --nologo -v minimal
        if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }
    }

    $exe = Join-Path $OutDir 'Tooba.Host.exe'
    if (-not (Test-Path $exe)) { throw "Host executable not found at $exe. Run without -NoBuild." }

    if (-not (Test-Path $StateDir)) { New-Item -ItemType Directory -Force -Path $StateDir | Out-Null }

    # Ephemeral process-scoped environment only; never written to any appsettings file.
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = $BaseUrl
    Remove-Item Env:Tooba__ShippingOneLevelCatalog__Enabled -ErrorAction SilentlyContinue
    if ($GateShippingOneLevelCatalog) {
        $env:Tooba__ShippingOneLevelCatalog__Enabled = 'true'
        Write-Step "ephemeral gate: Tooba__ShippingOneLevelCatalog__Enabled=true (process env only)"
    } else {
        Write-Step "ephemeral gate: ShippingOneLevelCatalog left at its disabled default"
    }
    Write-Step "ShippingRateAuthority and ShippingOperationalShadow are never set by this script (remain OFF)"

    $process = Start-Process -FilePath $exe `
        -ArgumentList '--contentRoot', $ContentRoot `
        -WorkingDirectory $OutDir `
        -PassThru `
        -RedirectStandardOutput $OutLog `
        -RedirectStandardError $ErrLog

    Write-Step "started pid=$($process.Id); waiting for /health on $BaseUrl"
    $health = Wait-ForHealth
    if ($null -eq $health) {
        Write-Step "ERROR: /health did not become ready. See $OutLog and $ErrLog"
        throw "Host did not become healthy within the timeout."
    }
    Write-Step "health OK: $health"

    $provenance = Test-Provenance
    Assert-DevelopmentProvenance $provenance

    Write-State ([pscustomobject]@{
        pid            = $process.Id
        port           = $Port
        url            = $BaseUrl
        executablePath = $exe
        outDir         = $OutDir
        contentRoot    = $ContentRoot
        gate           = if ($GateShippingOneLevelCatalog) { 'ShippingOneLevelCatalog=true' } else { 'defaults (all gates off)' }
        startedAt      = (Get-Date).ToString('o')
    })
    Write-Step "PID file written: $PidFile"
    Write-Step "rollback: run '-Action stop' then '-Action start' (without the gate) to return to the default gate state"
}

switch ($Action) {
    'status' { Show-Status }
    'stop' { [void](Stop-ManagedHost -AllowForce:$Force) }
    'start' { Start-ManagedHost }
    'restart' {
        [void](Stop-ManagedHost -AllowForce:$Force)
        Start-ManagedHost
    }
}
