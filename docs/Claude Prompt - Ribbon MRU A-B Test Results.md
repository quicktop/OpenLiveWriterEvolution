# Prompt for Claude: reassess the Ribbon MRU label bug after controlled A/B testing

Please reassess the Open Live Writer Evolution Ribbon MRU label bug using the new empirical results below. Do not repeat the already-refuted UICC-version or resource-language hypotheses. The goal is to identify the remaining difference between the working official 0.6.2 binary and a fresh build from the same source, or to determine that the native ApplicationMenu command-gallery behavior is not practically recoverable and document why.

## Repository and issue context

- Repository: `OpenLiveWriterEvolution`, branch `master`.
- Original investigation: `docs/Ribbon MRU Gallery Label Bug.md`.
- The affected pre-workaround controls were `SplitButtonGallery` instances in `Ribbon.ApplicationMenu` for local drafts and recent posts.
- Each MRU row routed to the correct post, but displayed the static markup label `Open Draft` / `Open post` instead of the runtime title.
- The current `master` workaround, commit `a486cd8f`, replaces those galleries with ordinary buttons that open the existing WinForms dialog.
- Controlled testing used the pre-workaround diagnostic commit `7b96ee3a`.

## Result 1: the modern UICC toolchain hypothesis is refuted

The exact upstream `Ribbon.xml` from initial/upstream-equivalent commit `00ec9e80` was compiled with:

```text
C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x86\UICC.exe
File version: 10.0.26100.8249
```

The resulting `Ribbon.bin` was compared with the `UIFILE/RIBBON_RIBBON` resource extracted from the official 2017 `OpenLiveWriter.Ribbon.dll` in `OpenLiveWriter-0.6.2-full.nupkg`.

Both were exactly:

```text
Length: 28020 bytes
SHA-256: 41E8A6C26FC7315E6F3F496B09193CE41F8C7BC97A3365ECC7263A10DEB9D922
```

Therefore, current Windows SDK UICC produces a byte-for-byte identical compiled Ribbon resource from the same XML. Pinning or vendoring an old UICC is not expected to fix the bug.

The pre-workaround `Ribbon.en-US.xml` was also compared line-by-line with the upstream XML and had zero semantic line differences.

## Result 2: UIFILE resource language is not the MRU-label root cause

The fork currently wraps the generated `Ribbon.rc`, including the `UIFILE`, in:

```rc
LANGUAGE LANG_CHINESE, SUBLANG_CHINESE_TRADITIONAL
#include ".\Ribbon.rc"
```

This means the English portable DLL contains a zh-TW (`LANGID 1028`) UIFILE even though `culture.cfg` is `en-US`. The official 0.6.2 DLL contains an en-US (`LANGID 1033`) UIFILE. This remains a separate localization/packaging defect.

However, a controlled UI A/B test showed that it does not cause the MRU-label bug:

### A build

- Pre-workaround commit `7b96ee3a`.
- Release/x86.
- Original fork resource wrapper: UIFILE language zh-TW.
- Created and saved a local draft titled `MRU A-B test title 20260712`.
- Opened File -> Open local draft gallery.
- Actual visible row label: `Open Draft`.

### B build

- Same managed binaries as A.
- Same `Ribbon.xml` and the same byte-identical `Ribbon.bin`.
- Only `OpenLiveWriter.Ribbon.dll` was relinked so the UIFILE resource language was en-US.
- Created and saved a local draft titled `MRU B en-US test title 20260712`.
- Opened File -> Open local draft gallery.
- Actual visible row labels: `Open Draft` for both cached test entries.

Thus switching the UIFILE from zh-TW to en-US does not restore runtime titles.

## Result 3: managed implementation in the official binary matches the source pattern

The official 0.6.2 `OpenLiveWriter.PostEditor.dll` was inspected with Mono.Cecil. Its `DraftPostItemsGalleryCommand` constructor and `LoadItems()` IL confirm the same relevant behavior as the source:

- Creates ten individual backing `Command` instances.
- Registers them via `CommandManager.Add(new CommandCollection(_commands))`.
- Assigns `LabelTitle = PostInfo.Title` at runtime.
- Adds `GalleryItem(PostInfo.Title, null, command)`.
- Calls the base `LoadItems()` to invalidate `UI_PKEY_ItemsSource`.

This did not reveal a hidden managed-code fix in the official release binary.

## Result 4: official documentation changes how the symptom should be interpreted

Microsoft's `IUISimplePropertySet::GetValue` command-gallery example returns `UI_PKEY_CommandId` for command-gallery items; it returns `UI_PKEY_Label` and `UI_PKEY_ItemImage` for item-gallery items. Therefore, the fact that `DynamicCommandGallery` places `UI_PKEY_Label` inside a command-gallery collection item does not mean the Ribbon framework must use it.

Also, `SplitButtonGallery` supports a `Type` attribute in general. The actual restriction is that `ApplicationMenu` supports command galleries only. The previous wording that SplitButtonGallery itself has no `Type` attribute is incorrect.

The diagnostic log from commit `7b96ee3a` remains important: the Ribbon framework queries `Enabled`, `LargeImageHighColor`, `LabelDescription`, and `ToolTipTitle` for each backing MRU command but never queries `UI_PKEY_Label`, even after successful label invalidation calls.

## Build and test results

The current `master` workaround was built in Release mode using `/m:1`.

```text
Release build: completed
x86 tests: 68 passed, 0 failed
English portable: dist/OpenLiveWriterEvolution-Portable-en-0.3.6.0.zip
zh-TW portable: dist/OpenLiveWriterEvolution-Portable-zh-TW-0.3.6.0.zip
```

No source files were changed during the A/B test.

## Additional build defects discovered

### 1. Parallel version-file generation race

The default build uses `/maxcpucount`. Every project imports `writer.build.targets`, whose `GenerateVersionFiles` target writes the same shared files:

```text
src/managed/GlobalAssemblyVersionInfo.cs
src/unmanaged/version.h
```

The parallel Release build reproducibly failed with `MSB3491` and `CS1504` because multiple projects concurrently created/read `GlobalAssemblyVersionInfo.cs`. Rebuilding with `/m:1` succeeded.

Please propose a correct MSBuild-level fix that generates the shared version files exactly once, rather than permanently disabling parallel compilation.

### 2. Installer packaging errors are swallowed

During a build, `src/managed/PostBuild.CreateInstaller/createinstaller.cmd` produced NuGet/Squirrel failures, including a missing `OpenLiveWriter..nupkg`, but continued printing:

```text
Created Writer NuGet package.
Created Open Live Writer setup file.
Created Writer Chocolatey Package
```

The overall build still returned exit code 0 and the portable ZIP packaging continued. The command file does not check `ERRORLEVEL` after NuGet, SyncReleases, Squirrel, MOVE, or Chocolatey packaging commands.

Please propose fail-fast error handling and distinguish normal application/portable builds from installer/release packaging so an installer failure cannot be reported as success.

There are also unresolved Newtonsoft.Json 10.0.0.0 versus 13.0.0.0 build warnings; assess whether the runtime binding redirects and copied DLL version are internally consistent.

## What I need from you

1. Update the diagnosis to explicitly mark these hypotheses as refuted:
   - modern UICC generates a materially different Ribbon binary;
   - zh-TW versus en-US UIFILE language causes the MRU label symptom.
2. Identify any remaining testable binary or runtime difference between the official working 0.6.2 package and the fresh pre-workaround build. Prioritize evidence from PE resources, assembly metadata/IL, COM registration/binding, Ribbon cache state, initialization order, and exact loaded-module paths.
3. Explain whether command labels inside an `ApplicationMenu` command gallery are supported as dynamic properties at all, using Microsoft documentation or Windows SDK sample behavior rather than inference.
4. Assess whether the official-binary observation should be revalidated with a screen recording/log and a clean Ribbon cache, because all controlled fresh builds reproduce the static-label behavior while the source and compiled Ribbon resource match.
5. Recommend whether to retain the current plain-button workaround. If proposing another native Ribbon implementation, it must be schema-valid inside `ApplicationMenu`, preserve correct per-row routing, and demonstrate dynamic labels in an actual build.
6. Provide implementation-ready fixes for the two build defects above, including exact files/targets to change and verification commands.

Do not claim a root cause unless it is supported by a reproducible differential test. Clearly separate confirmed facts, likely explanations, and proposed experiments.
