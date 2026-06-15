# Open Live Writer Evolution
Open Live Writer Evolution makes it easy to write, preview, and post to your blog.
This is a community-driven fork of Open Live Writer, focused on modern WordPress compatibility and long-term maintenance.

---

## What's Different from Open Live Writer

Open Live Writer Evolution diverges from the upstream [Open Live Writer](https://github.com/OpenLiveWriter/OpenLiveWriter) project in several important ways, all aimed at making the editor usable with modern WordPress sites and contemporary web themes.

### 1. IE11 Rendering Engine (Modern CSS Support)

The original Open Live Writer locks its embedded MSHTML editor to **IE9 emulation mode** (`IE=EmulateIE9`), because IE10+ removed *Element Behaviors* — an IE-specific COM extension used internally for table editing, image manipulation, and editable region management.

Open Live Writer Evolution upgrades MSHTML to **IE11 mode** at runtime by:

- Setting `HKCU\SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION` to `11001` (IE11 Edge mode) before any MSHTML component initializes (`ApplicationMain.cs`).
- Changing the `X-UA-Compatible` meta tag injected into the editor template from `IE=EmulateIE9` to `IE=11`.

**Why this matters:** IE9 has no support for `display: flex` or `display: grid`. Modern WordPress themes (including block-based Gutenberg layouts and themes such as tagDiv/Newspaper) rely on flexbox and CSS Grid for column layouts. In the original OLW, any content using these properties collapses to a single vertical column in both the Edit and Preview tabs, even though the live website renders it correctly. IE11 supports flexbox and grid natively, so the editor now reflects the actual page layout.

### 2. Improved WordPress Theme Detection

The original blog template detector (`BlogEditingTemplateDetector`) uses MSHTML DOM traversal to locate the post body region on a downloaded blog page. This approach fails with modern WordPress REST/Gutenberg page structures that no longer use the same landmark IDs and classes that older OLW versions expected.

Open Live Writer Evolution improves theme detection by:

- Rewriting `BlogPostRegionLocatorStrategy` to use **string-based GUID searching** instead of MSHTML DOM walking, making detection more resilient to markup changes.
- Adding permalink/ID-based URL targeting for WordPress so that the detector fetches a real single-post page rather than the homepage, which provides a more representative template.
- Processing downloaded template CSS files through `CssFlexGridRewriter` to normalize flex/grid rules that would otherwise confuse the IE rendering pipeline.
- Fixing null-dereference issues in `HTMLDocumentHelper` that caused silent failures when loading complex modern pages.

### 3. Background Color and Style Detection Fixes

- Fixed a race condition in `PostEditorMainControl` where the editor template HTML was assigned before the background color had been read from the downloaded theme, resulting in a white background overriding the theme color.
- Fixed CSS `media="not all"` attributes on theme stylesheets (a common WordPress trick to defer non-critical CSS) so that the editor loads the full theme styles rather than skipping them.

### 4. Build System Fix

- Updated `LocEdit.csproj` target framework from `v4.7.2` to `v4.8` to match the global `writer.build.settings`, resolving a `CS0246` build error in the localization editor tool.

### 5. Full Traditional Chinese (zh-TW) UI Localization

Open Live Writer Evolution is the first Open Live Writer build to ship a complete Traditional Chinese (正體中文) interface, covering both the Windows Ribbon toolbar and all application strings.

**Ribbon toolbar localization:**

All tabs, groups, and command buttons are translated into Traditional Chinese, including:

- Main tabs: Home (常用), Insert (插入), View (檢視)
- Contextual tabs: Video Tools (影片工具 — aspect ratio, widescreen/standard, view online), Table Tools (表格工具 — insert/move/delete rows and columns, cell options, custom table), Map Tools (地圖工具 — custom map, alignment), Tag Tools (標籤工具 — custom tags, provider management)

**Technical implementation:**

The Windows Ribbon Framework stores label strings in two places: ASCII labels compiled into the binary (`Ribbon.bin`), and Unicode overrides in a `STRINGTABLE` resource block inside the DLL. This project places the Traditional Chinese strings under a `LANGUAGE LANG_CHINESE, SUBLANG_CHINESE_TRADITIONAL` block in the RC file. Windows automatically applies the Chinese labels when the UI thread culture is zh-TW; English users fall back to the ASCII labels embedded in `Ribbon.bin`. A single DLL therefore serves both language variants without any runtime branching.

**Locale override (culture.cfg):**

Place a `culture.cfg` text file next to the executable containing a culture code (`zh-TW` or `en-US`) to force a specific UI language regardless of the operating system locale. The pre-built portable packages include this file so the interface language is correct out of the box.

---

## Changelog

### 2026-06-15
- **Full zh-TW Ribbon localization:** All Ribbon tabs, groups, and buttons translated to Traditional Chinese via `LANG_CHINESE_TRADITIONAL` STRINGTABLE in the Ribbon DLL. Single DLL serves both zh-TW and en-US with Windows resource language fallback.
- **Locale override (culture.cfg):** Added `culture.cfg` startup hook to force UI culture independently of the OS locale.
- **Portable builds split:** Separate zh-TW and English portable zips with version number in filename; `UserData\` cache excluded from packages.
- **HTML style thumbnail fix:** Fixed blank BMP thumbnails in the style gallery (`HtmlScreenCaptureCore`) by keeping the capture window visible but positioned off-screen rather than hidden.

### 2026-06-14
- **IE11 upgrade:** Force MSHTML to IE11 mode via `FEATURE_BROWSER_EMULATION` registry key; change `X-UA-Compatible` to `IE=11`. Flexbox and CSS Grid now render correctly in the Edit and Preview tabs.
- **Removed IE9 CSS emulation:** Removed `StylePreserver` inline-style rewriting and `CssFlexGridRewriter` injection from the editor template pipeline — no longer needed with IE11.
- **Build fix:** `LocEdit.csproj` target framework changed from `v4.7.2` to `v4.8`.

### Earlier (2026)
- Fixed WordPress theme detection using string-based GUID search in `BlogPostRegionLocatorStrategy`.
- Fixed background color detection order in `PostEditorMainControl`.
- Fixed `media="not all"` stylesheet suppression in downloaded blog templates.
- Null-safety improvements in `HTMLDocumentHelper.StringToHTMLDoc` and `ResetPath`.

---

## Installation

### Portable (No Install Required)

Download the appropriate zip from the [Releases](https://github.com/quicktop/OpenLiveWriterEvolution/releases) page:

| Package | Filename | Language |
|---------|----------|----------|
| Traditional Chinese | `OpenLiveWriterEvolution-Portable-zh-TW-*.zip` | Full zh-TW Ribbon and UI |
| English | `OpenLiveWriterEvolution-Portable-en-*.zip` | English UI, for non-Chinese OS |

Extract to any folder and run `OpenLiveWriter.exe`. Blog account settings are stored in the Windows registry (`HKCU\SOFTWARE\OpenLiveWriter`) and are shared across all builds on the same machine — no re-setup needed when switching versions.

### Build from Source

Clone or download this repository and build from source. See the **Building** section below.

---

## Contributing

Open Live Writer Evolution is an open source project and welcomes community contributions.
If you would like to help out then please see the [Contributing](CONTRIBUTING.md) guide.

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/)
to clarify expected behavior in our community.

For a list of known issues or to report bugs, see the [Issues](https://github.com/quicktop/OpenLiveWriterEvolution/issues) page.

---

## License

Open Live Writer Evolution uses the [MIT License](license.txt).

---

## History

The product that became Live Writer was originally created by a small, super-talented team of engineers including
JJ Allaire, Joe Cheng, Charles Teague, and Spike Washburn. The team was acquired by Microsoft
in 2006 and organized with the Spaces team. Becky Pezely joined the team and over time, the team grew and shipped
many popular releases of Windows Live Writer.

Microsoft concluded active development with Windows Live Writer 2012. In December 2015 Microsoft donated the code
to the .NET Foundation, and the community released it as Open Live Writer.

Open Live Writer Evolution is a further fork of Open Live Writer, continuing development with a focus on
modern blog platform compatibility, particularly WordPress.

---

## Building

Open Live Writer Evolution can be built by running `build.ps1` (PowerShell) or `build.cmd` found in this directory.

```powershell
# Debug build (default)
.\build.ps1

# Release build
$env:OLW_CONFIG = 'Release'; .\build.ps1
```

The solution is `src/managed/writer.sln`. If you see errors in Visual Studio, run `build.ps1` from the command prompt first.
The main program is `src/managed/OpenLiveWriter/ApplicationMain.cs`.
To run from Visual Studio, set the startup project to `OpenLiveWriter`.

**Binaries output:** `src/managed/bin/<Config>/i386/Writer/`

**Prerequisites:** Visual Studio 2017 or later (or Build Tools for Visual Studio) with the .NET Framework 4.6.1 Developer Pack and Desktop development with C++ workload.

---

## .NET Foundation

This project is based on code originally supported by the [.NET Foundation](http://www.dotnetfoundation.org).
