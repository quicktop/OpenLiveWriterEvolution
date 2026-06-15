# Cause powershell to fail on errors rather than keep going
$ErrorActionPreference = "Stop";

# Supported Visual Studio versions: VS2017, VS2019, VS2022, VS2026
# To override the C++ platform toolset, pass: /p:PlatformToolset=v142 (or v141, v143, v144)

@"

=======================================================
 Checking solution exists
=======================================================
"@

$solutionFile = "$PSSCRIPTROOT\src\managed\writer.sln"
if (-Not (Test-Path "$solutionFile" -PathType Leaf))
{
	"Unable to find solution file at $solutionFile"
	exit 100
}
"Solution found at '$solutionFile'"

@"

=======================================================
 Fetching MSBuild location
=======================================================
"@

# Use vswhere to find the latest Visual Studio installation
$vswherePath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vswherePath) {
    $visualStudioLocation = & $vswherePath -latest -property installationPath
} else {
    # Fallback: try VSSetup module
    Install-Module VSSetup -Scope CurrentUser -Force
    $visualStudioLocation = (Get-VSSetupInstance | Select-VSSetupInstance -Latest).InstallationPath
}

if (-Not $visualStudioLocation)
{
    "Visual Studio installation not found."
    "Ensure Visual Studio (2017 or later) or Build Tools for Visual Studio is installed."
    "These can be downloaded from https://visualstudio.microsoft.com/downloads/"
    exit 100
}

# Try "Current" path first (VS2019/VS2022/VS2026+), then fall back to versioned paths
$msBuildExe = $visualStudioLocation + "\MSBuild\Current\Bin\msbuild.exe"
IF (-Not (Test-Path -LiteralPath "$msBuildExe" -PathType Leaf))
{
    # Try VS2017 path
    $msBuildExe = $visualStudioLocation + "\MSBuild\15.0\Bin\msbuild.exe"
}
IF (-Not (Test-Path -LiteralPath "$msBuildExe" -PathType Leaf))
{
	"MSBuild not found at '$msBuildExe'"
	"In order to build OpenLiveWriter, Visual Studio 2017 or later must be installed."
	"Supported versions: VS2017, VS2019, VS2022, VS2026"
	"These can be downloaded from https://visualstudio.microsoft.com/downloads/"
	exit 101
}

"MSBuild.exe found at: '$msBuildExe'"

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

@"

=======================================================
 Ensureing nuget.exe exists
=======================================================
"@

$nugetPath = "$env:LocalAppData\NuGet"
$nugetExe = "$nugetPath\NuGet.exe"
if (-Not (Test-Path -LiteralPath "$nugetExe" -PathType Leaf))
{
	if (-Not (Test-Path -LiteralPath "$nugetPath" -PathType Container))
	{
		"Creating Directory '$nugetPath'"
		New-Item "$nugetPath" -Type Directory
	}
	"Downloading nuget.exe"
	Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile "$nugetExe"
}

"Nuget.exe found at: '$nugetExe'"

@"

=======================================================
 Ensure nuget packages exist
=======================================================
"@

$packageFolder = "$PSSCRIPTROOT\src\managed\packages"
if (Test-Path -LiteralPath $packageFolder)
{
    "Packages found at '$packageFolder'"
}
else
{
	"Running nuget restore"
	& $nugetExe restore $solutionFile
}

@"

=======================================================
 Check build type
=======================================================
"@

if (-Not (Test-Path env:OLW_CONFIG))
{
    "Environment variable OLW_CONFIG not set, setting to 'Debug'"
	$env:OLW_CONFIG = 'Debug'
}

"Using build '$env:OLW_CONFIG'"

@"

=======================================================
 Starting build
=======================================================
"@
Get-Date
$buildCommand = "`"$msBuildExe`" `"$solutionFile`" /nologo /maxcpucount /verbosity:minimal /p:Configuration=$env:OLW_CONFIG $ARGS"
"Running build command '$buildCommand'"
Clear-RibbonGeneratedFiles
Invoke-Expression "& $buildCommand"

if ($LASTEXITCODE -ne 0) {
    "Build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

$version = (Get-Content "$PSSCRIPTROOT\version.txt" -Raw).Trim()
$binDir  = "$PSSCRIPTROOT\src\managed\bin\$env:OLW_CONFIG\i386\Writer"

# ---------------------------------------------------------------------------
# zh-TW portable
# ---------------------------------------------------------------------------
@"

=======================================================
 Packaging zh-TW Portable  v$version
=======================================================
"@

$distZhTW = "$PSSCRIPTROOT\dist\OpenLiveWriterEvolution-Portable-zh-TW-$version"
if (Test-Path $distZhTW) {
    try { Remove-Item $distZhTW -Recurse -Force -ErrorAction Stop }
    catch { "Warning: could not fully clean dist dir (files may be in use). Copying over existing files." }
}
if (-not (Test-Path $distZhTW)) { New-Item $distZhTW -ItemType Directory | Out-Null }
Get-ChildItem $binDir | Where-Object { $_.Name -ne 'UserData' } | Copy-Item -Destination $distZhTW -Recurse -Force
$userDataZhTW = Join-Path $distZhTW 'UserData'
New-Item (Join-Path $userDataZhTW 'AppData\Roaming') -ItemType Directory -Force | Out-Null
New-Item (Join-Path $userDataZhTW 'AppData\Local') -ItemType Directory -Force | Out-Null
[System.IO.File]::WriteAllText((Join-Path $userDataZhTW 'portable.marker'), 'portable', [System.Text.Encoding]::ASCII)
[System.IO.File]::WriteAllText("$distZhTW\culture.cfg", "zh-TW", [System.Text.Encoding]::ASCII)
"Packaged: $distZhTW"

$zipZhTW = "$PSSCRIPTROOT\dist\OpenLiveWriterEvolution-Portable-zh-TW-$version.zip"
if (Test-Path $zipZhTW) { Remove-Item $zipZhTW -Force }
Compress-Archive -Path $distZhTW -DestinationPath $zipZhTW
"Zipped:   $zipZhTW"

# ---------------------------------------------------------------------------
# English portable
# ---------------------------------------------------------------------------
@"

=======================================================
 Packaging English Portable  v$version
=======================================================
"@

Build-EnglishRibbon

$distEn = "$PSSCRIPTROOT\dist\OpenLiveWriterEvolution-Portable-en-$version"
if (Test-Path $distEn) {
    try { Remove-Item $distEn -Recurse -Force -ErrorAction Stop }
    catch { "Warning: could not fully clean dist dir (files may be in use). Copying over existing files." }
}
if (-not (Test-Path $distEn)) { New-Item $distEn -ItemType Directory | Out-Null }
Get-ChildItem $binDir | Where-Object { $_.Name -ne 'UserData' } | Copy-Item -Destination $distEn -Recurse -Force
$userDataEn = Join-Path $distEn 'UserData'
New-Item (Join-Path $userDataEn 'AppData\Roaming') -ItemType Directory -Force | Out-Null
New-Item (Join-Path $userDataEn 'AppData\Local') -ItemType Directory -Force | Out-Null
[System.IO.File]::WriteAllText((Join-Path $userDataEn 'portable.marker'), 'portable', [System.Text.Encoding]::ASCII)
# Explicitly force English so users on zh-TW systems don't get Chinese UI
[System.IO.File]::WriteAllText("$distEn\culture.cfg", "en-US", [System.Text.Encoding]::ASCII)
"Packaged: $distEn"

$zipEn = "$PSSCRIPTROOT\dist\OpenLiveWriterEvolution-Portable-en-$version.zip"
if (Test-Path $zipEn) { Remove-Item $zipEn -Force }
Compress-Archive -Path $distEn -DestinationPath $zipEn
"Zipped:   $zipEn"
