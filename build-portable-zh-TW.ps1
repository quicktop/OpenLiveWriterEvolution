# Build script: Traditional Chinese (zh-TW) portable distribution
# Output: dist\OpenLiveWriterEvolution-Portable-zh-TW-<version>\  +  .zip

$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Version
# ---------------------------------------------------------------------------
$version = (Get-Content "$PSSCRIPTROOT\version.txt" -Raw).Trim()
$distName = "OpenLiveWriterEvolution-Portable-zh-TW-$version"
$distDir  = "$PSSCRIPTROOT\dist\$distName"
$zipPath  = "$PSSCRIPTROOT\dist\$distName.zip"

@"

=======================================================
 Building zh-TW Portable  (v$version)
=======================================================
"@

# ---------------------------------------------------------------------------
# Locate MSBuild
# ---------------------------------------------------------------------------
$solutionFile = "$PSSCRIPTROOT\src\managed\writer.sln"
if (-not (Test-Path $solutionFile -PathType Leaf)) {
    "Solution not found at $solutionFile"; exit 100
}

$vswherePath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vswherePath) {
    $vsLocation = & $vswherePath -latest -property installationPath
} else {
    Install-Module VSSetup -Scope CurrentUser -Force
    $vsLocation = (Get-VSSetupInstance | Select-VSSetupInstance -Latest).InstallationPath
}
if (-not $vsLocation) { "Visual Studio not found."; exit 100 }

$msBuildExe = "$vsLocation\MSBuild\Current\Bin\msbuild.exe"
if (-not (Test-Path $msBuildExe -PathType Leaf)) {
    $msBuildExe = "$vsLocation\MSBuild\15.0\Bin\msbuild.exe"
}
if (-not (Test-Path $msBuildExe -PathType Leaf)) { "MSBuild not found."; exit 101 }
"MSBuild: $msBuildExe"

$ribbonDir = "$PSSCRIPTROOT\src\unmanaged\OpenLiveWriter.Ribbon"
$ribbonGeneratedFiles = @(
    (Join-Path $ribbonDir "Ribbon.bin"),
    (Join-Path $ribbonDir "Ribbon.rc"),
    (Join-Path $ribbonDir "RibbonID.h")
)

function Clear-RibbonGeneratedFiles {
    foreach ($file in $ribbonGeneratedFiles) {
        if (Test-Path -LiteralPath $file) {
            Remove-Item -LiteralPath $file -Force
        }
    }
}

# ---------------------------------------------------------------------------
# Ensure NuGet + packages
# ---------------------------------------------------------------------------
$nugetExe = "$env:LocalAppData\NuGet\NuGet.exe"
if (-not (Test-Path $nugetExe -PathType Leaf)) {
    New-Item (Split-Path $nugetExe) -ItemType Directory -Force | Out-Null
    Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile $nugetExe
}
if (-not (Test-Path "$PSSCRIPTROOT\src\managed\packages" -PathType Container)) {
    & $nugetExe restore $solutionFile
}

# ---------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------
if (-not (Test-Path env:OLW_CONFIG)) { $env:OLW_CONFIG = 'Release' }
"Configuration: $env:OLW_CONFIG"
Get-Date
Clear-RibbonGeneratedFiles
Invoke-Expression "& `"$msBuildExe`" `"$solutionFile`" /nologo /maxcpucount /verbosity:minimal /p:Configuration=$env:OLW_CONFIG $ARGS"
if ($LASTEXITCODE -ne 0) { "Build failed ($LASTEXITCODE)"; exit $LASTEXITCODE }

# ---------------------------------------------------------------------------
# Package zh-TW portable
# ---------------------------------------------------------------------------
$binDir = "$PSSCRIPTROOT\src\managed\bin\$env:OLW_CONFIG\i386\Writer"

"Packaging: $distDir"
if (Test-Path $distDir) {
    try { Remove-Item $distDir -Recurse -Force -ErrorAction Stop }
    catch { "Warning: could not fully clean dist dir; copying over existing files." }
}
if (-not (Test-Path $distDir)) { New-Item $distDir -ItemType Directory | Out-Null }

Get-ChildItem $binDir | Where-Object { $_.Name -ne 'UserData' } | Copy-Item -Destination $distDir -Recurse -Force

$userDataDir = Join-Path $distDir 'UserData'
New-Item (Join-Path $userDataDir 'AppData\Roaming') -ItemType Directory -Force | Out-Null
New-Item (Join-Path $userDataDir 'AppData\Local') -ItemType Directory -Force | Out-Null
[System.IO.File]::WriteAllText((Join-Path $userDataDir 'portable.marker'), 'portable', [System.Text.Encoding]::ASCII)

# Traditional Chinese default — portable reads this file on startup
[System.IO.File]::WriteAllText("$distDir\culture.cfg", "zh-TW", [System.Text.Encoding]::ASCII)
"Created culture.cfg (zh-TW)"

# ---------------------------------------------------------------------------
# Compress to zip
# ---------------------------------------------------------------------------
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path $distDir -DestinationPath $zipPath
"Created: $zipPath"

@"

=======================================================
 Done — zh-TW portable ready
 Folder : $distDir
 Zip    : $zipPath
=======================================================
"@
