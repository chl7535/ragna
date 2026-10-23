param([ValidateSet('Android','iOS')][string]$Target = 'Android')
$ErrorActionPreference = 'Stop'
$ragnaRoot = Split-Path -Parent $PSScriptRoot
$ragnaProject = Join-Path $ragnaRoot 'Ragna'
$ragnaCli = Join-Path $PSScriptRoot 'UnityCli\unity.exe'
if (-not (Test-Path -LiteralPath $ragnaCli)) { throw 'Run Tools/Setup-Cli.ps1 first.' }
$ragnaStage = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) 'RagnaBuild-01a14433'))
if ($ragnaStage.ToCharArray() | Where-Object { [int]$_ -gt 127 }) { throw 'The temporary build path must contain ASCII characters only.' }
$ragnaMarker = Join-Path $ragnaStage '.ragna-build-workspace'
if ((Test-Path -LiteralPath $ragnaStage) -and -not (Test-Path -LiteralPath $ragnaMarker)) {
    throw 'The staging directory exists without the Ragna marker; it was not modified.'
}
if (Test-Path -LiteralPath $ragnaStage) {
    if ((Get-Item -LiteralPath $ragnaStage).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Build staging must not be a link.' }
    if ((Get-Content -Raw -LiteralPath $ragnaMarker).Trim() -ne $ragnaProject) { throw 'The staging marker belongs to a different source project.' }
}
New-Item -ItemType Directory -Force -Path $ragnaStage | Out-Null
$ragnaLock = Join-Path $ragnaStage 'Temp\UnityLockfile'
if (Test-Path -LiteralPath $ragnaLock) {
    # Interrupted Editor sessions can leave an unlocked file behind.
    try { $ragnaLockHandle = [IO.File]::Open($ragnaLock, 'Open', 'ReadWrite', 'None') }
    catch { throw 'The staging Unity project is already open. Close it before syncing inputs.' }
    $ragnaLockHandle.Dispose()
    Remove-Item -LiteralPath $ragnaLock -Force
}
Set-Content -LiteralPath $ragnaMarker -Value $ragnaProject
foreach ($ragnaFolder in @('Assets','Packages','ProjectSettings')) {
    $ragnaDestination = [IO.Path]::GetFullPath((Join-Path $ragnaStage $ragnaFolder))
    # Delete only our mirrored input folders, retaining Library/build caches.
    if (-not $ragnaDestination.StartsWith($ragnaStage + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe stage path.' }
    if (Test-Path -LiteralPath $ragnaDestination) {
        if ((Get-Item -LiteralPath $ragnaDestination).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Build input must not be a link.' }
        Remove-Item -LiteralPath $ragnaDestination -Recurse -Force
    }
    Copy-Item -LiteralPath (Join-Path $ragnaProject $ragnaFolder) -Destination $ragnaDestination -Recurse -Force
}
$ragnaMethod = if ($Target -eq 'Android') { 'Ragna.Editor.ProjectSetup.BuildAndroid' } else { 'Ragna.Editor.ProjectSetup.BuildIOS' }
$ragnaStageOutput = if ($Target -eq 'Android') { Join-Path $ragnaStage 'Builds\Android\Ragna.apk' } else { Join-Path $ragnaStage 'Builds\iOS' }
$ragnaLog = Join-Path $ragnaProject ('Logs\staged-' + $Target + '.log')
$ragnaProvenance = Join-Path $ragnaRoot ('Docs\BUILD_' + $Target + '.json')
& $ragnaCli build $ragnaStage --target $Target --execute-method $ragnaMethod --output-path $ragnaStageOutput --log-file $ragnaLog --provenance-path $ragnaProvenance --allow-dirty-build --no-tail --timeout 3600 --format json
if ($LASTEXITCODE -ne 0) { throw "Unity $Target build failed. See $ragnaLog" }
$ragnaOutputDirectory = Join-Path $ragnaProject ('Builds\' + $Target)
New-Item -ItemType Directory -Force -Path $ragnaOutputDirectory | Out-Null
if ($Target -eq 'Android') { Copy-Item -LiteralPath $ragnaStageOutput -Destination (Join-Path $ragnaOutputDirectory 'Ragna.apk') -Force }
else { Copy-Item -Path (Join-Path $ragnaStageOutput '*') -Destination $ragnaOutputDirectory -Recurse -Force }
Write-Output "Build copied to $ragnaOutputDirectory"
