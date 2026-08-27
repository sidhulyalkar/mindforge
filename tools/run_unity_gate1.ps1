param(
    [string]$UnityPath = "",
    [switch]$KeepPreviousReport
)

$ErrorActionPreference = "Stop"

$ExpectedVersion = "2022.3.76f1"
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$ProjectPath = Join-Path $RepoRoot "unity"
$ReportPath = Join-Path $RepoRoot "experiments\reports\unity-gate1-latest.json"
$LogPath = Join-Path $RepoRoot "experiments\reports\unity-gate1.log"

function Resolve-UnityEditor {
    param([string]$ExplicitPath)

    if ($ExplicitPath) {
        if (-not (Test-Path $ExplicitPath)) {
            throw "Unity editor not found at explicit path: $ExplicitPath"
        }
        return (Resolve-Path $ExplicitPath).Path
    }

    $candidates = New-Object System.Collections.Generic.List[string]
    if ($env:ProgramFiles) {
        $candidates.Add((Join-Path $env:ProgramFiles "Unity\Hub\Editor\$ExpectedVersion\Editor\Unity.exe"))
    }
    $programFilesX86 = [Environment]::GetEnvironmentVariable("ProgramFiles(x86)")
    if ($programFilesX86) {
        $candidates.Add((Join-Path $programFilesX86 "Unity\Hub\Editor\$ExpectedVersion\Editor\Unity.exe"))
    }

    $found = @($candidates | Where-Object { Test-Path $_ })
    if ($found.Count -eq 0) {
        throw @"
Unity $ExpectedVersion was not found.
Install exactly $ExpectedVersion through Unity Hub, or rerun with:
  .\tools\run_unity_gate1.ps1 -UnityPath 'C:\path\to\Unity.exe'
"@
    }

    return (Resolve-Path $found[0]).Path
}

$Unity = Resolve-UnityEditor -ExplicitPath $UnityPath
Write-Host "[Mindforge] Unity: $Unity"
Write-Host "[Mindforge] Project: $ProjectPath"
Write-Host "[Mindforge] Report: $ReportPath"

New-Item -ItemType Directory -Force -Path (Split-Path $ReportPath) | Out-Null

if (-not $KeepPreviousReport -and (Test-Path $ReportPath)) {
    Remove-Item $ReportPath -Force
}
if (Test-Path $LogPath) {
    Remove-Item $LogPath -Force
}

$arguments = @(
    "-batchmode",
    "-quit",
    "-projectPath", ('"{0}"' -f $ProjectPath),
    "-executeMethod", "Mindforge.Editor.CompetitionSceneAssembler.BuildAndValidate",
    "-logFile", ('"{0}"' -f $LogPath)
)

Write-Host "[Mindforge] Running Gate 1 assembler + validator..."
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -Wait -PassThru -NoNewWindow

if ($process.ExitCode -ne 0) {
    Write-Host "[Mindforge] Unity exited with code $($process.ExitCode)." -ForegroundColor Red
    if (Test-Path $LogPath) {
        Write-Host "[Mindforge] Last Unity log lines:" -ForegroundColor Yellow
        Get-Content $LogPath -Tail 80
    }
    exit $process.ExitCode
}

if (-not (Test-Path $ReportPath)) {
    Write-Host "[Mindforge] Unity returned success but did not produce the Gate 1 report." -ForegroundColor Red
    if (Test-Path $LogPath) { Get-Content $LogPath -Tail 80 }
    exit 20
}

$report = Get-Content $ReportPath -Raw | ConvertFrom-Json
if ($report.schema -ne "mindforge.unity_gate1.v1") {
    Write-Host "[Mindforge] Unexpected report schema: $($report.schema)" -ForegroundColor Red
    exit 21
}

Write-Host ""
Write-Host "MINDFORGE UNITY GATE 1"
Write-Host "Editor: $($report.editor_version)"
Write-Host "Scene:  $($report.scene_path)"
Write-Host "UTC:    $($report.generated_utc)"
Write-Host ""

foreach ($check in $report.checks) {
    $marker = if ($check.passed) { "PASS" } else { "FAIL" }
    $color = if ($check.passed) { "Green" } else { "Red" }
    Write-Host ("[{0}] {1} :: {2}" -f $marker, $check.name, $check.detail) -ForegroundColor $color
}

if ($report.editor_version -ne $ExpectedVersion) {
    Write-Host "[FAIL] Exact editor version required: expected $ExpectedVersion, observed $($report.editor_version)" -ForegroundColor Red
    exit 22
}

if (-not $report.passed) {
    Write-Host ""
    Write-Host "[Mindforge] GATE 1 FAILED. Do not proceed to Phantom combat." -ForegroundColor Red
    exit 23
}

Write-Host ""
Write-Host "[Mindforge] GATE 1 PASS. Editor-generated evidence is at:" -ForegroundColor Green
Write-Host "  $ReportPath" -ForegroundColor Green
Write-Host "[Mindforge] Unity import/assembly log:" -ForegroundColor Green
Write-Host "  $LogPath" -ForegroundColor Green
exit 0
