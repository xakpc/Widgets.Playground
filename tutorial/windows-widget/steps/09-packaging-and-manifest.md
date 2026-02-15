# Step 09 - Add Packaging and Manifest Declarations

This step declares the widget provider in package metadata so Windows can discover and activate it.

## Goal

After this step, the project should have:

- MSIX tooling enabled in project settings
- `Package.appxmanifest` with:
  - COM server extension
  - widget app extension definition
- CLSID consistency between provider code and manifest declarations

## Scope

This step adds packaging metadata and required assets.
Deploy/pin flow is covered in Step 10.

## Files Added or Updated

- `src/Xakpc.Widgets.Playground/Xakpc.Widgets.Playground.csproj`
- `src/Xakpc.Widgets.Playground/Package.appxmanifest`
- `src/Xakpc.Widgets.Playground/Properties/launchSettings.json`
- `src/Xakpc.Widgets.Playground/Assets/icon.png`
- `src/Xakpc.Widgets.Playground/Assets/Square150x150Logo.png`
- `src/Xakpc.Widgets.Playground/Assets/Square44x44Logo.png`
- `src/Xakpc.Widgets.Playground/Assets/StoreLogo.png`
- `src/Xakpc.Widgets.Playground/Assets/screenshots/CatFactScreenshot.png`

## Project Packaging Properties

`Xakpc.Widgets.Playground.csproj` includes:

```xml
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<RuntimeIdentifiers>win-x64;win-arm64</RuntimeIdentifiers>
<EnableMsixTooling>true</EnableMsixTooling>
<GenerateAppInstallerFile>false</GenerateAppInstallerFile>
<AppxPackageSigningEnabled>false</AppxPackageSigningEnabled>
```

These settings keep packaging minimal for local tutorial iteration.
`RuntimeIdentifier` is important here: MSIX packaging cannot build as ProcessorArchitecture-neutral (`AnyCPU`) for this app host model.
`RuntimeIdentifiers` keeps both x64 and ARM64 available; `RuntimeIdentifier` sets the default local publish target.

For Visual Studio integration, include:

```xml
<ItemGroup Condition="'$(DisableMsixProjectCapabilityAddedByProject)' != 'true' and '$(EnableMsixTooling)' == 'true'">
  <ProjectCapability Include="Msix" />
</ItemGroup>
```

This helps Visual Studio recognize the project as MSIX-capable for single-project packaging features.

## Manifest Structure (What Matters)

`Package.appxmanifest` includes two critical extension blocks:

1. `windows.comServer`
   - declares executable launch + COM class id
2. `windows.appExtension` (`com.microsoft.windows.widgets`)
   - declares widget provider metadata and definitions

### COM Extension Snippet

```xml
<com:Extension Category="windows.comServer">
  <com:ComServer>
    <com:ExeServer
      Executable="Xakpc.Widgets.Playground.exe"
      Arguments="-RegisterProcessAsComServer"
      DisplayName="Widgets Playground Provider">
      <com:Class Id="4C363B3C-0E2D-4ACF-A797-40DE1B6D4FAD" DisplayName="WidgetProvider" />
    </com:ExeServer>
  </com:ComServer>
</com:Extension>
```

### Widget App Extension Snippet

```xml
<uap3:Extension Category="windows.appExtension">
  <uap3:AppExtension
    Name="com.microsoft.windows.widgets"
    DisplayName="Widgets Playground"
    Id="xakpc.widgets.playground">
    <uap3:Properties>
      <WidgetProvider>
        <ProviderIcons>
          <Icon Path="Assets\icon.png" />
        </ProviderIcons>
        <Activation>
          <CreateInstance ClassId="4C363B3C-0E2D-4ACF-A797-40DE1B6D4FAD" />
        </Activation>
        <Definitions>
          <Definition
            Id="CatFact_Widget"
            DisplayName="Cat Fact"
            Description="Random cat facts"
            IsCustomizable="false">
            <Capabilities>
              <Capability><Size Name="small" /></Capability>
              <Capability><Size Name="medium" /></Capability>
              <Capability><Size Name="large" /></Capability>
            </Capabilities>
            <ThemeResources>
              <Icons>
                <Icon Path="Assets\icon.png" />
              </Icons>
              <Screenshots>
                <Screenshot Path="Assets\screenshots\CatFactScreenshot.png" DisplayAltText="Cat Fact Widget" />
              </Screenshots>
              <DarkMode>
                <Icons>
                  <Icon Path="Assets\icon.png" />
                </Icons>
                <Screenshots>
                  <Screenshot Path="Assets\screenshots\CatFactScreenshot.png" DisplayAltText="Cat Fact Widget" />
                </Screenshots>
              </DarkMode>
              <LightMode>
                <Icons>
                  <Icon Path="Assets\icon.png" />
                </Icons>
                <Screenshots>
                  <Screenshot Path="Assets\screenshots\CatFactScreenshot.png" DisplayAltText="Cat Fact Widget" />
                </Screenshots>
              </LightMode>
            </ThemeResources>
          </Definition>
        </Definitions>
      </WidgetProvider>
    </uap3:Properties>
  </uap3:AppExtension>
</uap3:Extension>
```

## GUID Consistency Checklist

The same provider GUID must match in all places:

- `src/Xakpc.Widgets.Playground/WidgetProvider.cs` `[Guid("...")]`
- `src/Xakpc.Widgets.Playground/Package.appxmanifest`:
  - `<com:Class Id="...">`
  - `<CreateInstance ClassId="...">`

`Program.cs` uses `typeof(WidgetProvider).GUID`, so it stays aligned automatically with the provider attribute.

## Intricate Details That Commonly Break Discovery

These are the subtle issues that can make the widget not appear even when code compiles:

- COM argument mismatch:
  - manifest must launch with `-RegisterProcessAsComServer`
  - `Program.cs` must actually check for that exact argument
- Executable name mismatch:
  - `<com:ExeServer Executable="...">` must match produced app host exe name
  - for this project: `Xakpc.Widgets.Playground.exe`
- Platform/architecture mismatch:
  - packaged build cannot stay ProcessorArchitecture-neutral
  - use concrete runtime identifier (`win-x64` or `win-arm64`)
- Missing `MsixPackage` launch profile:
  - single-project MSIX debugging depends on `launchSettings.json`
- Missing screenshot/icon assets in `ThemeResources`:
  - invalid or missing paths can hide widget from picker
- Stale deployment state:
  - old installed package can keep old manifest metadata
  - after manifest changes, rebuild and redeploy before re-checking picker

## Fast Verification Checklist (Before Opening Widget Picker)

1. Build succeeds for targeted runtime (`win-x64` in this tutorial).
2. `WidgetProvider.cs` `[Guid("...")]` equals both manifest GUID locations.
3. Manifest contains both extensions:
   - `windows.comServer`
   - `windows.appExtension` with `Name="com.microsoft.windows.widgets"`
4. `launchSettings.json` includes `MsixPackage` profile with COM server argument.
5. Asset paths referenced by manifest exist in project:
   - provider icon
   - widget screenshot
6. Package redeployed after latest manifest edits.

## Launch Profile Requirement (Single-Project MSIX)

For packaged debugging in Visual Studio, include an `MsixPackage` launch profile in:

- `src/Xakpc.Widgets.Playground/Properties/launchSettings.json`

Recommended profile:

```json
{
  "profiles": {
    "Provider": {
      "commandName": "MsixPackage",
      "commandLineArgs": "-RegisterProcessAsComServer"
    }
  }
}
```

Without this profile, Visual Studio can report that packaged debugging is not configured for single-project MSIX.

Note: in single-project MSIX flow, the old separate "Deploy package" command may not appear the same way as in `.wapproj` projects.
The expected path is selecting an `MsixPackage` profile and running/debugging from Visual Studio.

## Image Placeholders

- `[IMAGE: add-packaging-project.png]` - Packaging setup (single-project packaging in this tutorial path).
- `[IMAGE: manifest-extensions-block.png]` - COM + widget extension sections.

## Verify Step 09

Run:

```powershell
dotnet build Xakpc.Widgets.Playground.sln
```

Expected result: successful build with manifest and packaging metadata included.

## Common Mistakes

- mismatched GUID between provider and manifest
- forgetting `-RegisterProcessAsComServer` argument in COM executable server
- wrong widget definition id in manifest vs provider expectations
- building packaged output without a concrete runtime identifier (`win-x64` / `win-arm64`)
- missing/invalid screenshot paths in `ThemeResources` (widget may not show in picker)
- changing manifest but checking picker before redeploying updated package

## Next Step

Step 10 covers build, deploy, and manual validation in the Widget Board.
