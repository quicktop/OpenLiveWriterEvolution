# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Project:** Open Live Writer Evolution (`OpenLiveWriterEvolution`) — a community fork of Open Live Writer focused on modern WordPress compatibility. GitHub: https://github.com/quicktop/OpenLiveWriterEvolution

## Build Commands

```powershell
# Debug build (default)
.\build.ps1

# Release build
$env:OLW_CONFIG = 'Release'; .\build.ps1

# Pass extra MSBuild flags (e.g., C++ toolset override for VS2022)
.\build.ps1 /p:PlatformToolset=v143
```

`build.ps1` auto-locates MSBuild via `vswhere.exe` and downloads NuGet.exe to `%LocalAppData%\NuGet\` if missing. The solution is `src/managed/writer.sln`.

**Binaries output:** `src/managed/bin/<Config>/i386/Writer/`

**Run the application after building:**
```cmd
run.cmd
```

**Prerequisites:** Visual Studio 2017+ (or Build Tools) with .NET Framework 4.6.1 Developer Pack.

## Tests

Tests live in `src/managed/OpenLiveWriter.Tests/` and use **NUnit** with **ApprovalTests**.

```powershell
# Build tests first, then run via Visual Studio Test Explorer or:
$vstest = "C:\Program Files (x86)\Microsoft Visual Studio\...\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
& $vstest src\managed\bin\Debug\i386\Writer\OpenLiveWriter.Tests.dll
```

ApprovalTests stores approved HTML snapshots alongside test files (`.approved.html`). When a test produces new output, a diff tool opens automatically; approve by copying the received file to the approved file.

## Architecture

### Platform Constraints
- **Windows only**, **x86 only** (32-bit), **.NET Framework 4.6.1**
- Requires IE/MSHTML for the post editor — the HTML editing surface is a COM-hosted MSHTML control, not a modern WebView
- Unmanaged C++ ribbon lives in `src/unmanaged/OpenLiveWriter.Ribbon/`

### Startup Flow
`ApplicationMain.Main` → `SingleInstanceApplicationManager.Run` → `LaunchAction`:
- First instance: initializes app environment, registers Squirrel update handlers, shows splash screen, calls `PostEditorLifetimeManager.Initialize()`, then `ApplicationLauncher.LaunchBloggingForm()`
- Additional instances: forwards args to the first instance via IPC

### Key Assembly Boundaries

| Assembly | Role |
|---|---|
| `OpenLiveWriter` | Startup, single-instance management, Squirrel auto-update |
| `OpenLiveWriter.PostEditor` | Main blogging UI, post editor window, content plugin lifecycle |
| `OpenLiveWriter.HtmlEditor` | MSHTML-based WYSIWYG editor surface |
| `OpenLiveWriter.BlogClient` | Blog platform adapters (WordPress, Blogger, TypePad, etc.) |
| `OpenLiveWriter.CoreServices` | Cross-cutting utilities: settings (registry-backed), logging, file helpers, HTTP |
| `OpenLiveWriter.ApplicationFramework` | Ribbon/toolbar framework, command pattern, sidebar panels |
| `OpenLiveWriter.Extensibility` | Plugin/ContentSource interfaces — the public extension point |
| `OpenLiveWriter.Api` | Stable public plugin API (v1.1.0.0) |
| `OpenLiveWriter.Interop` | Win32 P/Invoke; `OpenLiveWriter.Interop.Mshtml` wraps MSHTML COM typelib |
| `OpenLiveWriter.Localization` | 70+ locale string tables; `Res.Get(StringId.*)` is the lookup pattern |

### Content Plugin System
`ContentSourceManager` (initialized at startup) discovers plugins implementing `ContentSource` or `SmartContentSource` from `OpenLiveWriter.Extensibility`. Built-in plugins (video, maps, tags, etc.) are in `OpenLiveWriter.InternalWriterPlugin`. Third-party plugins drop DLLs into the `Plugins/` subdirectory next to the exe.

### Settings Storage
All user settings are stored in the Windows registry under `HKCU\SOFTWARE\OpenLiveWriter`. The abstraction layer is `SettingsPersisterHelper` wrapping `RegistrySettingsPersister` — there is no file-based config.

### Blog Client Pattern
Each blog platform implements `IBlogClient` in `OpenLiveWriter.BlogClient`. Clients handle credential storage, post CRUD, image upload, and category retrieval. Blogger uses OAuth2 via Google APIs; WordPress uses XML-RPC or Atom.

### Version
Defined in `version.txt` (current: 0.6.3.0). The build system auto-generates `src/managed/GlobalAssemblyVersionInfo.cs` and `src/unmanaged/version.h` from it.
