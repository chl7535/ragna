# Use the verified project-local Unity CLI when available, with the game as cwd.
$ragnaRoot = Split-Path -Parent $PSScriptRoot
$ragnaCli = Join-Path $PSScriptRoot 'UnityCli\unity.exe'
if (-not (Test-Path -LiteralPath $ragnaCli)) { throw 'Run .\Tools\Setup-Cli.ps1 first.' }
Push-Location (Join-Path $ragnaRoot 'Ragna')
try {
    & $ragnaCli @args
    $ragnaExitCode = $LASTEXITCODE
} finally { Pop-Location }
exit $ragnaExitCode
