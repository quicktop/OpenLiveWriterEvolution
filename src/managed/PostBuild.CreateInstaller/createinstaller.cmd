@ECHO OFF

REM This step packages a Squirrel-based installer/Chocolatey package using Microsoft's
REM original OpenLiveWriter release infrastructure (a specific Azure blob storage account
REM for SyncReleases, plus %OLW_SIGN% code-signing). This fork does not have access to
REM that infrastructure and only ships the portable ZIP built by build.ps1, so this step
REM is skipped by default. Set OLW_BUILD_INSTALLER=1 to opt in (e.g. if you have your own
REM signing cert and release feed configured).
IF NOT "%OLW_BUILD_INSTALLER%" == "1" (
  ECHO Skipping installer/Squirrel/Chocolatey packaging ^(set OLW_BUILD_INSTALLER=1 to enable^).
  EXIT /B 0
)

PUSHD "%~dp0..\..\..\"

CALL getversion.cmd
IF ERRORLEVEL 1 (
  ECHO ERROR: getversion.cmd failed.
  POPD
  EXIT /B 1
)

IF "%OLW_CONFIG%" == "" (
  echo %%OLW_CONFIG%% not set, will default to 'Debug'
  set OLW_CONFIG=Debug
)

IF NOT EXIST "%LocalAppData%\Nuget\Nuget.exe" (
  ECHO ERROR: Nuget.exe missing from %LocalAppData%\Nuget\Nuget.exe
  POPD
  EXIT /B 1
)

"%LocalAppData%\Nuget\Nuget.exe" pack .\OpenLiveWriter.nuspec -version %dottedVersion% -basepath src\managed\bin\%OLW_CONFIG%\i386\Writer
IF ERRORLEVEL 1 (
  ECHO ERROR: nuget pack of OpenLiveWriter.nuspec failed.
  POPD
  EXIT /B 1
)
ECHO Created Writer NuGet package.

.\src\managed\packages\squirrel.windows.1.4.4\tools\SyncReleases.exe -url=https://olw.blob.core.windows.net/stable/Releases/ -r=.\Releases
IF ERRORLEVEL 1 (
  ECHO ERROR: SyncReleases.exe failed.
  POPD
  EXIT /B 1
)

.\src\managed\packages\squirrel.windows.1.4.4\tools\Squirrel.exe -i .\src\managed\OpenLiveWriter.PostEditor\Images\Writer.ico %OLW_SIGN% --no-msi --releasify .\OpenLiveWriter.%dottedVersion%.nupkg
IF ERRORLEVEL 1 (
  ECHO ERROR: Squirrel.exe --releasify failed.
  POPD
  EXIT /B 1
)

MOVE .\Releases\Setup.exe .\Releases\OpenLiveWriterSetup.exe
IF ERRORLEVEL 1 (
  ECHO ERROR: MOVE of Setup.exe failed.
  POPD
  EXIT /B 1
)
ECHO Created Open Live Writer setup file.

::Build Chocolatey package. Suppress package analysis since Chocolatey powershell generates verbose warnings.
"%LocalAppData%\Nuget\Nuget.exe" pack .\OpenLiveWriter.Install.nuspec -version %dottedVersion% -basepath Releases -nopackageanalysis
IF ERRORLEVEL 1 (
  ECHO ERROR: nuget pack of OpenLiveWriter.Install.nuspec failed.
  POPD
  EXIT /B 1
)
ECHO Created Writer Chocolatey Package

POPD
EXIT /B 0
