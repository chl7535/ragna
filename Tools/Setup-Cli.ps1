param([string]$Version = '1.0.0-beta.13')
$ErrorActionPreference = 'Stop'
$ragnaBase = 'https://public-cdn.cloud.unity3d.com/hub/prod/cli/'
$ragnaManifest = Invoke-RestMethod -Uri ($ragnaBase + $Version + '/latest.json')
if ($ragnaManifest.version -ne $Version) { throw 'Unexpected Unity CLI manifest version.' }
$ragnaBinary = $ragnaManifest.binaries.'win32-x64'
if (-not $ragnaBinary) { throw 'Windows x64 CLI is unavailable.' }
$ragnaBinDir = Join-Path $PSScriptRoot 'UnityCli'
New-Item -ItemType Directory -Force -Path $ragnaBinDir | Out-Null
$ragnaDownload = Join-Path $ragnaBinDir 'unity.download'
Invoke-WebRequest -Uri ($ragnaBase + $Version + '/' + $ragnaBinary.filename) -OutFile $ragnaDownload
$ragnaActualHash = (Get-FileHash -LiteralPath $ragnaDownload -Algorithm SHA256).Hash
if ($ragnaActualHash -ne $ragnaBinary.sha256) { throw 'Unity CLI checksum mismatch. Download was not installed.' }
$ragnaExecutable = Join-Path $ragnaBinDir 'unity.exe'
Move-Item -LiteralPath $ragnaDownload -Destination $ragnaExecutable -Force
& $ragnaExecutable --version
# No global PATH or existing Unity CLI is modified.
