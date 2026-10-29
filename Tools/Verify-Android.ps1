param([string]$ApkPath, [string]$AndroidPlayerPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.26f1\Editor\Data\PlaybackEngines\AndroidPlayer')
$ErrorActionPreference = 'Stop'
$ragnaRoot = Split-Path -Parent $PSScriptRoot
if (-not $ApkPath) { $ApkPath = Join-Path $ragnaRoot 'Ragna\Builds\Android\Ragna.apk' }
$ragnaApk = Get-Item -LiteralPath $ApkPath
$ragnaBuildTools = Join-Path $AndroidPlayerPath 'SDK\build-tools\36.0.0'
$ragnaJava = Join-Path $AndroidPlayerPath 'OpenJDK\bin\java.exe'
$ragnaSigner = Join-Path $ragnaBuildTools 'lib\apksigner.jar'
$ragnaAapt = Join-Path $ragnaBuildTools 'aapt2.exe'
$ragnaSignature = & $ragnaJava -jar $ragnaSigner verify --verbose --print-certs $ragnaApk.FullName 2>&1
if ($LASTEXITCODE -ne 0) { throw ('APK signature verification failed: ' + ($ragnaSignature -join "`n")) }
$ragnaBadging = & $ragnaAapt dump badging $ragnaApk.FullName 2>&1
if ($LASTEXITCODE -ne 0) { throw 'APK manifest inspection failed.' }
$ragnaManifest = $ragnaBadging -join "`n"
if ($ragnaManifest -notmatch "package: name='com.ragnastudio.ragna'") { throw 'Unexpected application ID.' }
if ($ragnaManifest -notmatch "native-code: 'arm64-v8a'") { throw 'ARM64 ABI is missing.' }
if ($ragnaManifest -notmatch "minSdkVersion:'26'") { throw 'Unexpected minimum Android version.' }
$ragnaXml = & $ragnaAapt dump xmltree $ragnaApk.FullName --file AndroidManifest.xml 2>&1
if ($LASTEXITCODE -ne 0 -or ($ragnaXml -join "`n") -notmatch 'android:screenOrientation\(0x0101001e\)=0\b') { throw 'Landscape activity orientation is missing.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$ragnaArchive = [IO.Compression.ZipFile]::OpenRead($ragnaApk.FullName)
try {
    foreach ($ragnaEntry in @('AndroidManifest.xml','lib/arm64-v8a/libil2cpp.so','lib/arm64-v8a/libunity.so')) {
        if ($null -eq $ragnaArchive.GetEntry($ragnaEntry)) { throw "Missing APK entry: $ragnaEntry" }
    }
} finally { $ragnaArchive.Dispose() }
$ragnaReport = [ordered]@{
    verifiedAt = [DateTimeOffset]::UtcNow.ToString('o')
    file = 'Ragna/Builds/Android/Ragna.apk'
    bytes = $ragnaApk.Length
    sha256 = (Get-FileHash -LiteralPath $ragnaApk.FullName -Algorithm SHA256).Hash
    result = 'PASS: APK signature, application ID, ARM64 IL2CPP, Unity libraries, API 26 minimum and landscape orientation'
    signature = @($ragnaSignature | ForEach-Object { "$_" })
    manifest = @($ragnaBadging | Where-Object { $_ -match '^\s*(package:|minSdkVersion:|targetSdkVersion:|native-code:|launchable-activity:|uses-feature:.*landscape)' } | ForEach-Object { "$_" })
    orientation = 'landscape (Android screenOrientation=0)'
    deviceTested = $false
    note = 'Local test build. Signature verification does not imply release signing or physical-device validation.'
}
$ragnaReport | ConvertTo-Json -Depth 5 | Set-Content -Encoding utf8 (Join-Path $ragnaRoot 'Docs\APK_VERIFICATION.json')
$ragnaReport | ConvertTo-Json -Depth 5
