# Step 11 - Troubleshooting

This step documents the most common failures when building, deploying, and validating the widget.

## Goal

After this step, you should be able to:

- quickly map a symptom to likely root cause
- run targeted checks in the right files
- fix issues without random trial-and-error edits

## Scope

Troubleshooting guidance only.
No code or manifest changes are introduced in this step.

## File Added

- `tutorial/windows-widget/steps/11-troubleshooting.md`

## Quick Triage Order

When something breaks, check in this order:

1. Build passes (`dotnet build`).
2. Launch profile is `MsixPackage` with COM arg.
3. CLSID is aligned across provider + manifest.
4. Manifest extension blocks are valid.
5. Manifest asset paths exist.
6. Package identity/deployment state is not stale.

Do not edit generated manifests in `build/bin/...`.
Always edit `src/Xakpc.Widgets.Playground/Package.appxmanifest`.

## Symptom -> Cause -> Fix

### 1) Widget does not appear in Add Widgets picker

Likely causes:

- missing/invalid widget app extension block
- stale deployed package after manifest edits
- missing manifest-referenced icon/screenshot assets

Checks:

1. Open `src/Xakpc.Widgets.Playground/Package.appxmanifest`.
2. Confirm `uap3:Extension Category="windows.appExtension"` exists.
3. Confirm `uap3:AppExtension Name="com.microsoft.windows.widgets"` exists.
4. Confirm referenced files exist:
   - `Assets\icon.png`
   - `Assets\screenshots\CatFactScreenshot.png`
5. Re-run with `MsixPackage` profile to redeploy.

### 2) Widget appears in picker but pinning/activation fails

Likely causes:

- COM activation mismatch (CLSID or argument mismatch)
- wrong COM executable name in manifest

Checks:

1. In `src/Xakpc.Widgets.Playground/WidgetProvider.cs`, copy `[Guid("...")]`.
2. In `src/Xakpc.Widgets.Playground/Package.appxmanifest`, verify same GUID in:
   - `<com:Class Id="...">`
   - `<CreateInstance ClassId="...">`
3. Verify `<com:ExeServer ... Arguments="-RegisterProcessAsComServer">`.
4. Verify `<com:ExeServer Executable="Xakpc.Widgets.Playground.exe">`.
5. Verify `Program.cs` checks for exact arg `-RegisterProcessAsComServer`.

### 3) Visual Studio says `launchSettings.json` / `MsixPackage` is required

Likely causes:

- missing `MsixPackage` profile
- malformed profile JSON

Checks:

1. Open `src/Xakpc.Widgets.Playground/Properties/launchSettings.json`.
2. Confirm at least one profile has:
   - `"commandName": "MsixPackage"`
   - `"commandLineArgs": "-RegisterProcessAsComServer"`
3. Use that profile when starting (`F5`/`Ctrl+F5`).

### 4) Build works, but package/deploy behavior is inconsistent across machines

Likely causes:

- architecture mismatch (`AnyCPU` expectations)
- runtime identifier not concrete

Checks:

1. In `src/Xakpc.Widgets.Playground/Xakpc.Widgets.Playground.csproj`, verify:
   - `<RuntimeIdentifier>win-x64</RuntimeIdentifier>` (or chosen target)
   - `<RuntimeIdentifiers>win-x64;win-arm64</RuntimeIdentifiers>`
2. In Visual Studio, set solution platform to `x64` or `ARM64` (not AnyCPU).

### 5) Widget renders but background is missing

Likely causes:

- template expects `backgroundImageDataUri` but provider payload does not include it
- image asset not available in package output

Checks:

1. Open `src/Xakpc.Widgets.Playground/Templates/CatFactTemplate.json`.
2. Confirm:
   - `"backgroundImage": "${backgroundImageDataUri}"`
3. Open `src/Xakpc.Widgets.Playground/WidgetProvider.cs`.
4. Confirm payload includes `backgroundImageDataUri` in `BuildDataPayload(...)`.
5. Confirm `Assets\background-small.png` is included in project content items.

### 6) Widget is blank or partially rendered

Likely causes:

- invalid template JSON
- data payload keys do not match template bindings
- JSON string escaping issue in dynamic text

Checks:

1. Verify bindings in template:
   - `${fact}`
   - `${errorMessage}`
   - `${backgroundImageDataUri}`
2. Verify payload keys in provider `BuildDataPayload(...)` match those names.
3. Verify provider uses escaping helper before injecting string values.

### 7) Refresh button behavior does not match expectations by size

Current expected behavior:

- `small`: no refresh button
- `medium` and `large`: refresh button visible
- all sizes refresh on activation

Checks:

1. In `CatFactTemplate.json`, confirm refresh action condition:
   - `"$when": "${$host.widgetSize != \"small\"}"`
2. In `WidgetProvider.cs`, confirm `Activate(...)` always triggers:
   - `_ = RefreshAndUpdateAsync(widget.WidgetId);`

### 8) Manifest edits seem ignored

Likely causes:

- editing generated manifest under `build/bin/...`
- testing old installed package identity

Checks:

1. Edit only:
   - `src/Xakpc.Widgets.Playground/Package.appxmanifest`
2. Verify installed package identity:

```powershell
Get-AppxPackage | Where-Object { $_.Name -like "*WidgetsPlayground*" } | Select-Object Name, PackageFullName, Publisher
```

3. If stale identities exist, remove old package and redeploy current one.

## Minimal Verification Commands

Build:

```powershell
dotnet build Xakpc.Widgets.Playground.sln
```

Package identity check:

```powershell
Get-AppxPackage | Where-Object { $_.Name -like "*WidgetsPlayground*" } | Select-Object Name, PackageFullName, Publisher
```

## Common Anti-Patterns

- changing multiple variables at once (GUID + identity + assets) before retest
- editing `build/bin/.../AppxManifest.xml` and expecting persistent results
- assuming `AnyCPU` is valid for this packaged widget provider flow
- debugging picker issues before confirming manifest asset paths are valid

## Next Step

Step 12 adds lightweight production extras (logging and resilient fallback guidance) without introducing framework complexity.
