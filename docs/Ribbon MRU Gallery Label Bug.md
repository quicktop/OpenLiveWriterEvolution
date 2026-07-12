# Windows Ribbon Framework bug: Command-type gallery items in ApplicationMenu never refresh their Label

## Project context

Open Live Writer Evolution (`OpenLiveWriterEvolution`) is a community fork of the discontinued Microsoft **Open Live Writer** desktop blog editor. Relevant facts:

- Windows-only, x86-only (32-bit), .NET Framework 4.6.1, WinForms.
- The ribbon toolbar is implemented via the native **Windows Ribbon Framework** (`IUIFramework`/`IUICommandHandler` COM APIs, `UIRibbon.dll`), not a WinForms-drawn ribbon. The ribbon layout is authored in an XML markup file (`Ribbon.xml`), compiled at build time by Microsoft's `UICC.exe` (UI Command Compiler, part of the Windows SDK) into a native resource DLL (`OpenLiveWriter.Ribbon.dll`) linked into the app.
- The managed side implements `IUICommandHandler` in `CommandManager` (`src/managed/OpenLiveWriter.ApplicationFramework/CommandManager.cs`), which dispatches `UpdateProperty`/`Execute` calls to individual `Command` objects (`Command.cs`, same folder).
- Upstream project: https://github.com/OpenLiveWriter/OpenLiveWriter (archived, last release `0.6.2.0`, May 2017). This fork's `master` is otherwise unrelated/ahead in many other areas but the specific files below are **byte-for-byte identical** to upstream's `0.6.2.0` tag (confirmed via `git diff` after normalizing UTF-8 BOM).

## The bug

In the "File" menu (the `Ribbon.ApplicationMenu`/backstage panel), there are two entries — "Open local draft" and "Open recent post" — implemented as `SplitButtonGallery` controls. Hovering/expanding either one reveals a flyout list of up to 10 rows (the user's most recent drafts/posts), each of which is supposed to show that post's real title and a WordPress/blog icon.

**Actual behavior**: every row shows a static placeholder — literally the string `"Open Draft"` / `"Open post"` (or the zh-TW equivalent `"開啟草稿"`/`"開啟文章"`) — never the real title, regardless of what the app code sets at runtime. Clicking a row still opens the *correct* post (routing works), only the **displayed label is wrong**.

## Relevant code (pre-fix, matches upstream `0.6.2.0` exactly)

`src/managed/OpenLiveWriter.PostEditor/Commands/DraftPostItemsGalleryCommand.cs`:

```csharp
public class DraftPostItemsGalleryCommand : DynamicCommandGallery
{
    private Command[] _commands = new Command[MaxItems]; // MaxItems = 10
    private int draftCmdStart = (int)CommandId.OpenDraftMRU0; // 10 pre-declared CommandIds, 23307-23316

    public DraftPostItemsGalleryCommand(IBlogPostEditingSite postEditingSite, CommandManager commandManager, bool isPost)
        : base(isPost ? CommandId.OpenPostSplit : CommandId.OpenDraftSplit)
    {
        for (int i = 0; i < _commands.Length; i++)
        {
            _commands[i] = new Command((CommandId)(i + draftCmdStart));
            _commands[i].Execute += new EventHandler(DraftPostItemsGalleryCommand_Execute);
            _commands[i].CommandBarButtonStyle = CommandBarButtonStyle.Provider;
            _commands[i].On = false;
        }
        commandManager.Add(new CommandCollection(_commands)); // NB: doesn't wire Command.StateChanged (see "attempt 1")
        commandManager.Add(this);
    }

    public override void LoadItems()
    {
        items.Clear();
        postInfo = (_isPost ? PostListCache.RecentPosts : PostListCache.Drafts); // real PostInfo[] from disk

        for (int i = 0; i < _commands.Length && i < postInfo.Length; i++)
        {
            PostInfo v = postInfo[i];
            _commands[i].On = true;
            _commands[i].Enabled = true;
            _commands[i].LabelTitle = v.Title;              // <-- real title assigned here
            _commands[i].LabelDescription = v.BlogName;
            _commands[i].TooltipTitle = v.Title;
            _commands[i].TooltipDescription = v.BlogName;
            _commands[i].LargeImage = /* WordPress or generic icon */;
            items.Add(new GalleryItem(v.Title, null, _commands[i])); // GalleryItem.Label ALSO gets the real title
        }
        base.LoadItems(); // marks UI_PKEY_ItemsSource pending
    }
}
```

`DynamicCommandGallery` (`src/managed/OpenLiveWriter.PostEditor/Commands/DynamicCommandGallery.cs`) is a custom `GalleryCommand<Command>` subclass that overrides `GetPropVariant(UI_PKEY_ItemsSource, ...)` to build a **"command type" (`UI_COMMANDTYPE_ACTION`) collection**: for each `GalleryItem`, it emits a `SimplePropertySet` containing `UI_PKEY_Label` (from `item.Label`), `UI_PKEY_ItemImage`, `UI_PKEY_CommandId`, `UI_PKEY_CommandType=ACTION`, `UI_PKEY_CategoryId`.

Ribbon markup (`src/unmanaged/OpenLiveWriter.Ribbon/Ribbon.xml`), inside `<Ribbon.ApplicationMenu><ApplicationMenu CommandName="cmdFileMenu"><MenuGroup>`:

```xml
<SplitButtonGallery CommandName="cmdOpenDraftSplit" ApplicationModes="0,1"/>
<SplitButtonGallery CommandName="cmdOpenPostSplit" ApplicationModes="0,1"/>
```

And the 10 backing commands per gallery, each with only a **static** placeholder label declared in markup:

```xml
<Command Name="cmdOpenDraftMRU0" Symbol="OpenDraftMRU0" Id="23307" LabelTitle="Open Draft"/>
<!-- ...MRU1..MRU9, same pattern... -->
```

## Diagnosis performed (all empirically verified, not guessed)

1. **Ruled out invalidation/event-wiring bugs.** `commandManager.Add(new CommandCollection(_commands))` does not subscribe `Command.StateChanged`, so changes never reach `Command.FlushPendingInvalidations()` → `IUIFramework.InvalidateUICommand()`. Fixed to `commandManager.Add(_commands)` (the `params Command[]` overload, which *does* subscribe). **No change in observed behavior.**

2. **Added temporary diagnostic logging** directly in the framework glue (`Command.LabelTitle` setter, `Command.FlushPendingInvalidations`, `CommandManager.UpdateProperty`), filtered to the 10 MRU `CommandId`s, logging every `InvalidateUICommand` call (with HRESULT) and every `UpdateProperty` callback the framework actually makes. Built a debug package, had the end user (who has real drafts/posts) open the flyout, and sent back the log. Result — **the framework only ever calls back `UpdateProperty` for `UI_PKEY_Enabled`, `UI_PKEY_LargeImageHighColor`, `UI_PKEY_LabelDescription`, and `UI_PKEY_ToolTipTitle` on each individual MRU command. It never once queries `UI_PKEY_Label`,** even though `InvalidateUICommand(commandId, ..., UI_PKEY_Label)` was called (and returned `hr=0`/S_OK) for every command, every time `LoadItems()` ran. So the framework accepts the invalidation request but simply never re-fetches that specific property for gallery-collection commands.

3. **Tried switching to an "item type" (`Type="Items"`) collection** instead of command-type, mirroring a different, verified-working gallery in the same codebase (`BlogProviderButtonsGallery`, an `InRibbonGallery Type="Items"` on a normal ribbon tab, where each `GalleryItem`'s `Label`/`Image` are supplied directly and rendered correctly — no per-item `CommandId` needed). Result:
   - `<SplitButtonGallery ... Type="Items">` inside `Ribbon.ApplicationMenu` → **rejected by the markup compiler**: `error SC1053: DTD/ '{...}SplitButtonGallery' 'Type'`.
   - Switched to `<DropDownGallery ... Type="Items">` instead (this control **does** support `Type="Items"` — confirmed working elsewhere in the same `Ribbon.xml`, e.g. `cmdSelectBlog`, a blog-account picker dropdown on a normal tab, which correctly shows dynamic per-row blog names). But when the *same* `DropDownGallery Type="Items"` element is placed inside `<Ribbon.ApplicationMenu>` instead of a normal `<Tab><Group>`, the compiler **again rejects it**: `error SC1053: DTD/ '{...}DropDownGallery' 'Type'`.
   - **Correction (per follow-up review):** the original wording here — "`SplitButtonGallery` has no `Type` attribute in the schema at all" — was imprecise. `SplitButtonGallery` supports `Type` in general; the actual restriction demonstrated by both compiler errors is narrower: **`Ribbon.ApplicationMenu` only accepts command-type (`UI_COMMANDTYPE_ACTION`) galleries, regardless of which gallery control is used.** Item-type galleries (`Type="Items"`) are rejected specifically because of the ApplicationMenu host, not because of the control choice.
   - Also independently discovered that `GalleryCommand<T>.PerformExecute()`'s base implementation unconditionally does `(uint)currentValue.PropVariant.Value`, which threw `NullReferenceException` when a `SplitButtonGallery` row was clicked after switching the C# side to item-type semantics while the control was still schema-mandated command-type — i.e. `currentValue` came back empty/null for that mismatched configuration. This crash is why the item-type experiment was reverted quickly.

4. **Ruled out Debug-vs-Release build configuration as the cause.** The project's default `build.ps1` builds `Debug` unless `$env:OLW_CONFIG='Release'` is set. Rebuilt the *original, unmodified* (byte-identical-to-upstream) `DraftPostItemsGalleryCommand`/`Ribbon.xml` explicitly in `Release` and reproduced the exact same bug (still shows the static "Open Draft" placeholder for a real, on-disk test draft). So it is not a debug-only artifact/timing issue.

5. **Compared against the official upstream binary.** Downloaded the official pre-built `OpenLiveWriter.exe` from https://github.com/OpenLiveWriter/OpenLiveWriter/releases (confirmed via file properties: version `0.6.2.0`, the same tag diffed in step 0). The user ran it **on the same machine, same Windows version**, and the MRU flyout **appeared to work correctly** there — real titles showed up. This observation is now in tension with steps 6–8 below and has not been independently re-verified with a screen recording or a clean portable profile; treat it as unconfirmed rather than as an established fact.

## Follow-up investigation (second pass, via a different agent with binary-level tooling)

A second, independent investigation was run specifically to test the "build toolchain" hypothesis from step 5, using PE/IL-level comparison tools this pass didn't have access to. Its results:

6. **The modern-UICC-toolchain hypothesis is refuted.** The exact upstream `Ribbon.xml` (from this fork's initial commit, equivalent to the files diffed in step 0) was compiled with the Windows 11 SDK's `UICC.exe` (`C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x86\UICC.exe`, file version `10.0.26100.8249`). The resulting `Ribbon.bin` was compared against the `UIFILE/RIBBON_RIBBON` resource extracted directly from the official 2017 `OpenLiveWriter.Ribbon.dll` (from `OpenLiveWriter-0.6.2-full.nupkg`). **Both are byte-for-byte identical: 28,020 bytes, matching SHA-256.** A modern Windows SDK's `UICC.exe` produces an identical compiled ribbon resource from identical XML. Pinning/vendoring an older `UICC.exe` is not expected to change anything.

7. **A separate, real localization defect was found and ruled out as the cause.** This fork's `Ribbon.rc` wraps the generated `UIFILE` in `LANGUAGE LANG_CHINESE, SUBLANG_CHINESE_TRADITIONAL`, so the **English** portable build's `OpenLiveWriter.Ribbon.dll` actually embeds a **zh-TW** (`LANGID 1028`) `UIFILE` resource, even though `culture.cfg` says `en-US`. (The official 0.6.2 DLL correctly embeds an en-US/`LANGID 1033` `UIFILE`.) This is worth fixing separately as a packaging bug, but a controlled A/B test — two builds from the same managed binaries and the same byte-identical `Ribbon.bin`, differing only in whether the linked `OpenLiveWriter.Ribbon.dll`'s `UIFILE` resource was tagged zh-TW or en-US — showed **both builds exhibit the identical MRU-label bug** (`Open Draft` placeholder for real, freshly-saved test drafts in both cases). UIFILE resource language is not the root cause of the MRU-label symptom.

8. **The official binary's managed code matches the source exactly.** The official 0.6.2 `OpenLiveWriter.PostEditor.dll` was disassembled (Mono.Cecil). Its `DraftPostItemsGalleryCommand` constructor and `LoadItems()` IL show the identical pattern already documented above: ten backing `Command` instances, `CommandManager.Add(new CommandCollection(_commands))`, `LabelTitle = PostInfo.Title` assigned at runtime, `GalleryItem(PostInfo.Title, null, command)`, then the base `LoadItems()` invalidating `UI_PKEY_ItemsSource`. No hidden managed-code fix exists in the official release binary.

9. **Microsoft's own `IUISimplePropertySet::GetValue` sample documentation indicates this may not be a bug at all.** For command-gallery items, the documented pattern returns `UI_PKEY_CommandId` (so the framework can resolve the *referenced command's own, already-known* properties); `UI_PKEY_Label`/`UI_PKEY_ItemImage` are the documented pattern for **item**-gallery items specifically. In other words: putting `UI_PKEY_Label` inside a command-gallery collection entry (as `DynamicCommandGallery`/`LoadCommandSimplePropertySet` does — see the code excerpt above) is not something the framework is documented to consult. Combined with the diagnostic log from step 2 (framework queries `Enabled`, `LargeImageHighColor`, `LabelDescription`, `ToolTipTitle` per row, but **never** `Label`), this reframes the whole investigation: **this may be the Windows Ribbon Framework working exactly as designed** — command-type gallery entries are meant to reference pre-existing, already-labeled commands, not carry ad hoc runtime strings — rather than a bug, regression, or toolchain quirk.

## Current status: the official-binary observation is the one loose thread

Every other explanation has now been tested and refuted: not the C# wiring, not the toolchain/`UICC.exe` version, not Debug-vs-Release, not the UIFILE resource language, and the managed IL is identical between this fork and the official binary. Per Microsoft's documented `IUISimplePropertySet` contract, the observed behavior (label frozen from markup, never re-queried) looks like **expected framework behavior for command-type ApplicationMenu galleries**, not a defect.

That leaves one unresolved, unreproduced data point: the user's single observation that the official `0.6.2.0` binary showed real titles in the same flyout, on the same machine. **This has not been re-verified carefully** (no screen recording, no confirmation the flyout was actually expanded via the same click path rather than showing a hover/hint panel, no clean/fresh portable profile). Given everything upstream of it (source, compiled ribbon resource, IL) is now confirmed identical, the most likely explanations, in rough order of plausibility, are:
- **Observational error** — e.g. what was seen was the distinct native `<RecentItems>`/`cmdMRUList` control (a different feature in the same File menu, proven elsewhere in this codebase to support per-item labels correctly) rather than the `SplitButtonGallery` flyout, or a hover/tooltip state was mistaken for the expanded list.
- **Stale Windows Ribbon Framework state cache** — `IUIFramework` can persist customizations via `SaveSettingsToStream`/`LoadSettingsFromStream`; worth ruling out by testing on a machine/profile that has never run any version of this app, or by checking whether this app persists ribbon state anywhere and clearing it.
- Something neither investigation has found yet.

**Recommendation:** keep the shipped plain-button workaround (commit `a486cd8f`). Don't sink further effort into the native gallery flyout unless the official-binary observation is re-confirmed under controlled conditions (screen recording, fresh profile, confirming the exact control being interacted with).

## Build defects found during the follow-up investigation (unrelated to the ribbon bug, both fixed)

While investigating the above, two independent build-infrastructure defects surfaced. Both have been fixed on `master`:

**1. Parallel version-file generation race.** `writer.build.targets`' `GenerateVersionFiles` target (`BeforeTargets="CoreCompile"`) is imported directly by every managed `.csproj` (25 projects). Under `/maxcpucount`, multiple projects reach `CoreCompile` concurrently and each independently re-enters this target, racing to read `version.txt` and write the two shared output files (`GlobalAssemblyVersionInfo.cs`, `src/unmanaged/version.h`). This reproducibly caused `MSB3491`/`CS1504` under parallel Release builds. **Fix:** the file-write logic now runs inside a `RoslynCodeTaskFactory` inline task (`GenerateVersionFilesLocked`) guarded by a named, machine-wide `System.Threading.Mutex`, serializing the writes across parallel MSBuild worker processes; it also only writes when content actually changed, so unaffected projects' incremental builds stay stable. Fixing this also exposed a second, previously-masked ordering bug: `MarketXmlGenerator.csproj` referenced `..\GlobalAssemblyVersionInfo.cs` directly via a hardcoded (unguarded) `<Compile Include>` without importing `writer.build.targets` at all, relying entirely on some *other* project having generated the file first. It now imports `writer.build.targets` like every other project. Verified via three consecutive from-scratch builds (deleting both generated files first) with `/maxcpucount`, all succeeding.

**2. Installer packaging errors were being swallowed.** `src/managed/PostBuild.CreateInstaller/createinstaller.cmd` (run automatically as a `PostBuildEvent`, i.e. on every solution build including plain `build.ps1` runs) calls `nuget.exe pack`, `SyncReleases.exe`, `Squirrel.exe --releasify`, `MOVE`, and a second `nuget.exe pack`, none of which were checked for `ERRORLEVEL`. Since the script always fell through to `POPD` and exited 0, failures were invisible. This pipeline targets Microsoft's original release infrastructure (a specific Azure blob storage account for `SyncReleases`, plus a code-signing cert via `%OLW_SIGN%`) that this fork does not have access to — neither `build.ps1` nor `.github/workflows/release.yml` need or consume its output; both only package the raw build output directory into a portable ZIP. **Fix:** the script now checks `ERRORLEVEL` after every step and exits non-zero on real failure, but is **skipped by default** (prints a one-line notice and exits 0 immediately) unless `OLW_BUILD_INSTALLER=1` is set — so it no longer silently fails on every normal/portable build, and if someone does set up proper signing/release infrastructure and opts in, a real failure will now correctly fail the build instead of being reported as success.

**3. Newtonsoft.Json version was internally inconsistent (the warnings Codex flagged as a side note).** `OpenLiveWriter.PostEditor.csproj` had an explicit, hardcoded `<Reference>` pinned to the older `Newtonsoft.Json.10.0.2` package, while `OpenLiveWriter.BlogClient.csproj` (and, transitively, the actual copy that lands in the shared output directory) used `Newtonsoft.Json.13.0.1`. Because a direct/"primary" project reference always wins MSBuild's conflict resolution, this produced `MSB3277` unresolved-conflict warnings on every build, plus **7 of 8** `app.config` files across the solution had binding redirects capped at `10.0.0.0` (one, `OpenLiveWriter.Tests`, even had two *conflicting* entries — `9.0.0.0` and `10.0.0.0` — left over from prior NuGet upgrades that appended rather than updated, apparently due to a publicKeyToken casing mismatch) while the assembly actually deployed to `bin\...\Writer\Newtonsoft.Json.dll` is `13.0.0.0`. **Fix:** updated `OpenLiveWriter.PostEditor.csproj`'s reference to `Newtonsoft.Json.13.0.1`, removed the stale duplicate entry in `OpenLiveWriter.Tests\app.config`, and updated every binding redirect across the solution's `app.config` files to `0.0.0.0-13.0.0.0 → 13.0.0.0`, matching what's actually shipped. Verified: zero `Newtonsoft.Json`-related build warnings after the fix (previously several per build), and `Newtonsoft.Json.dll` in both `bin\Debug` and `bin\Release` output confirmed at assembly version `13.0.0.0`.

## Repo pointers (if the agent has repo access)

- Fork: this repo (branch `master`), workaround fix in commit `a486cd8f` after diagnostic commit `7b96ee3a` (now reverted/cleaned up). Build-defect fixes: `writer.build.targets`, `src/managed/MarketXmlGenerator/MarketXmlGenerator.csproj`, `src/managed/PostBuild.CreateInstaller/createinstaller.cmd`, `src/managed/OpenLiveWriter.PostEditor/OpenLiveWriter.PostEditor.csproj`, and the `app.config` files under `src/managed/{OpenLiveWriter,OpenLiveWriter.PostEditor,BlogRunner,BlogRunner.Core,BlogRunnerGui,OpenLiveWriter.UnitTest,OpenLiveWriter.Tests}`.
- Upstream for comparison: https://github.com/OpenLiveWriter/OpenLiveWriter, tag `0.6.2.0`.
- Key files: `src/managed/OpenLiveWriter.ApplicationFramework/{Command,CommandManager}.cs`, `src/managed/OpenLiveWriter.PostEditor/Commands/{DynamicCommandGallery,GalleryCommand,SelectGalleryCommand,RecentItemsCommand}.cs` (the last one wraps the native `<RecentItems>` control mentioned in the "current status" section above), `src/unmanaged/OpenLiveWriter.Ribbon/Ribbon.xml`, `src/unmanaged/OpenLiveWriter.Ribbon/OpenLiveWriter.Ribbon.vcxproj` (native project that invokes `UICC.exe`).
