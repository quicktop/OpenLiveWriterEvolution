# Build script: English portable distribution
# Output: dist\OpenLiveWriterEvolution-Portable-en-<version>\  +  .zip

$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Version
# ---------------------------------------------------------------------------
$version = (Get-Content "$PSSCRIPTROOT\version.txt" -Raw).Trim()
$distName = "OpenLiveWriterEvolution-Portable-en-$version"
$distDir  = "$PSSCRIPTROOT\dist\$distName"
$zipPath  = "$PSSCRIPTROOT\dist\$distName.zip"

@"

=======================================================
 Building English Portable  (v$version)
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

$ribbonProject = "$PSSCRIPTROOT\src\unmanaged\OpenLiveWriter.Ribbon\OpenLiveWriter.Ribbon.vcxproj"
$ribbonDir = Split-Path $ribbonProject
$repoSafeDirectory = $PSSCRIPTROOT -replace "\\", "/"
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

function Build-EnglishRibbon {
    $englishRibbonMarkup = Join-Path $ribbonDir "Ribbon.en-US.generated.xml"
    $englishRibbonSource = Join-Path $ribbonDir "Ribbon.en-US.xml"
    try {
        if (Test-Path -LiteralPath $englishRibbonSource) {
            $ribbonMarkupFile = $englishRibbonSource
        }
        else {
            "Preparing English ribbon markup"
            $ribbonMarkup = & git -c "safe.directory=$repoSafeDirectory" -C "$PSSCRIPTROOT" show "00ec9e80:src/unmanaged/OpenLiveWriter.Ribbon/Ribbon.xml"
            if ($LASTEXITCODE -ne 0) {
                throw "Unable to read English Ribbon.xml from git history."
            }

            [System.IO.File]::WriteAllLines($englishRibbonMarkup, $ribbonMarkup, [System.Text.Encoding]::UTF8)
            $ribbonMarkupFile = $englishRibbonMarkup
        }

        Clear-RibbonGeneratedFiles
        "Building English ribbon resource"
        & $msBuildExe $ribbonProject /nologo /verbosity:minimal /target:Rebuild /p:Configuration=$env:OLW_CONFIG /p:Platform=Win32 "/p:RibbonMarkupFile=$ribbonMarkupFile" $ARGS
        if ($LASTEXITCODE -ne 0) {
            throw "English ribbon build failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        Clear-RibbonGeneratedFiles
        if (Test-Path -LiteralPath $englishRibbonMarkup) {
            Remove-Item -LiteralPath $englishRibbonMarkup -Force
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
Build-EnglishRibbon

# ---------------------------------------------------------------------------
# Package English portable
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

# Explicitly force English so the app doesn't pick up the user's system locale (e.g. zh-TW)
[System.IO.File]::WriteAllText("$distDir\culture.cfg", "en-US", [System.Text.Encoding]::ASCII)
"Created culture.cfg (en-US)"

# ---------------------------------------------------------------------------
# Compress to zip
# ---------------------------------------------------------------------------
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path $distDir -DestinationPath $zipPath
"Created: $zipPath"

@"

=======================================================
 Done — English portable ready
 Folder : $distDir
 Zip    : $zipPath
=======================================================
"@
