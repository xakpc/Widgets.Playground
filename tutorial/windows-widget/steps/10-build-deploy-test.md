# Step 10 - Build, Deploy, and Test the Widget

This step is the execution runbook. Most setup work is already done in Steps 01-09.

## Goal

After this step, you should be able to:

- build the solution for your machine architecture
- deploy/run through single-project MSIX flow in Visual Studio
- pin the widget and confirm runtime behavior in the Widget Board

## Scope

This step is validation and test flow only.
No new provider or manifest code is introduced here.

## File Added

- `tutorial/windows-widget/steps/10-build-deploy-test.md`

## Preflight Checklist

Before running:

1. `src/Xakpc.Widgets.Playground/Properties/launchSettings.json` includes `MsixPackage` profiles.
2. `src/Xakpc.Widgets.Playground/Package.appxmanifest` has both required extensions:
   - `windows.comServer`
   - `windows.appExtension` with `Name="com.microsoft.windows.widgets"`
3. `src/Xakpc.Widgets.Playground/WidgetProvider.cs` GUID matches both manifest GUID references.
4. Visual Studio is set to a concrete architecture (`x64` or `ARM64`).
5. `src/Xakpc.Widgets.Playground/Xakpc.Widgets.Playground.csproj` uses:
   - `<OutputType>WinExe</OutputType>`

## Final Project-Type Switch (Important)

Step 10 is where the project should move from console-style output to Windows app host output:

- use `WinExe` for `OutputType`
- keep COM activation argument flow unchanged (`-RegisterProcessAsComServer`)

Why this matters:

- removes console-window app-host behavior from final packaged runs
- aligns with final widget-provider packaging/runtime expectations

## Build

Run:

```powershell
dotnet build Xakpc.Widgets.Playground.sln
```

Expected result: successful build before launch/deploy testing.

## Deploy and Launch (Visual Studio)

Single-project MSIX flow is profile-based:

1. Open `Xakpc.Widgets.Playground.sln` in Visual Studio.
2. Set startup project to `Xakpc.Widgets.Playground`.
3. Set solution configuration to `Debug`.
4. Set solution platform to your target architecture:
   - `x64` on most developer desktops
   - `ARM64` on ARM devices
5. Select launch profile:
   - `Provider on launch` (recommended for real host activation flow)
   - `Provider` (optional; starts with the same COM argument without `doNotLaunchApp`)
6. Start debugging (`F5`) or start without debugging (`Ctrl+F5`).

Notes:

- In single-project MSIX, the old separate **Deploy package** command may not appear like `.wapproj` workflows.
- Launching with an `MsixPackage` profile performs package deployment as part of the run/debug flow.

## Pin and Verify in Widget Board

1. Open Widget Board (`Win+W`).
2. Open **Add widgets**.
3. Find **Random Cat Fact** and pin it.
4. Wait for first refresh (initial loading text should change to a fact).

## Runtime Validation Scenarios

Verify all of these:

1. Background rendering:
   - widget shows the designed background image behind text.
2. Refresh action visibility:
   - `small`: no refresh button
   - `medium`/`large`: refresh button is visible
3. Manual refresh behavior:
   - click **Refresh** on `medium` or `large`
   - fact updates (or shows refresh error text while preserving last fact)
4. Activate refresh behavior:
   - switch away from Widget Board, then open it again
   - widget refresh runs on activation (including `small`, which has no button)
5. Persistence behavior:
   - close/reopen board or restart provider process
   - last known fact is restored from `CustomState` before next refresh completes

## Quick Failure Checks

If widget is missing or not updating:

1. Reconfirm GUID consistency between provider and manifest.
2. Reconfirm `-RegisterProcessAsComServer` is present in `MsixPackage` profile args.
3. Rebuild and redeploy after manifest or template changes.
4. Ensure asset paths in manifest exist and are copied in project content.
   - `StoreLogo.png`, `Square150x150Logo.png`, `Square44x44Logo.png`
   - provider icon and widget screenshot assets

## Symptom -> Likely Cause (MSIX Intricacies)

Use this table before deep debugging:

| Symptom | Most likely cause | First check |
| --- | --- | --- |
| No `MsixPackage` profile in VS | MSIX capability/tooling not recognized | `EnableMsixTooling=true` and `ProjectCapability Include="Msix"` in `.csproj` |
| “launchSettings.json was not found / MsixPackage required” | missing or wrong launch profile | `src/Xakpc.Widgets.Playground/Properties/launchSettings.json` |
| Widget absent from picker | app extension metadata invalid or stale deployment | `windows.appExtension` block + redeploy |
| Widget in picker but pin/activation fails | COM registration mismatch | CLSID and `-RegisterProcessAsComServer` alignment |
| Widget pins but never updates | provider not activated or action path broken | `Program.cs` arg gate + COM `Executable`/`Arguments` |
| Old metadata still appears after edits | stale installed package identity | verify `Identity Name`/`Publisher`, then redeploy active identity |
| Build/deploy works only on one machine arch | architecture mismatch | solution platform and `RuntimeIdentifier` (`win-x64` vs `win-arm64`) |

## Identity/Cache Reset Tip

When metadata changes still do not show up after redeploy, verify installed package identity:

```powershell
Get-AppxPackage | Where-Object { $_.Name -like "*WidgetsPlayground*" } | Select-Object Name, PackageFullName, Publisher
```

If multiple old identities/builds exist, remove stale packages and redeploy the current one.

## Image Placeholders

- `[IMAGE: widget-picker-catfact.png]` - Widget picker showing Random Cat Fact.
- `[IMAGE: pinned-widget-live.png]` - Pinned widget with loaded fact and background.

## Verify Step 10

Completion criteria:

1. Build succeeds.
2. Widget is visible in picker and can be pinned.
3. Runtime validation scenarios above pass.

## Common Mistakes

- using `AnyCPU`/neutral packaging expectations instead of concrete architecture.
- expecting old `.wapproj` deploy UI in single-project MSIX flow.
- testing stale package state after manifest updates.
- validating only one size and missing size-specific action behavior.

## Next Step

Step 11 adds a focused troubleshooting guide for common widget discovery and runtime failures.
