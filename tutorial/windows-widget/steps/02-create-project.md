# Step 02 - Create and Configure the Base Project

This step converts the starter console project into a Windows widget-ready project.

## Goal

After this step, the project should:

- target Windows explicitly
- include runtime identifiers for desktop architectures
- reference the Windows App SDK package required for widget provider APIs
- still stay simple (single project, flat structure)

## Scope

We only change project configuration in this step.
No provider lifecycle code is implemented yet.

## Project Changes

Update `src/Xakpc.Widgets.Playground/Xakpc.Widgets.Playground.csproj`:

- Set Windows target framework:
  - `net10.0-windows10.0.22000.0`
- Set minimum target platform:
  - `10.0.22000.0`
- Add runtime identifiers:
  - `win-x64`
  - `win-arm64`
- Add packages:
  - `Microsoft.WindowsAppSDK`

These settings move the project from generic console build to Windows widget-capable build.

Note:
- the final tutorial project also sets a default `RuntimeIdentifier` (`win-x64`) during packaging setup in Step 09.

## Base `.csproj` Snippet (Copy/Paste)

Use this as the target structure for Step 02:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0-windows10.0.22000.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.22000.0</TargetPlatformMinVersion>
    <Platforms>x64;ARM64</Platforms>
    <RuntimeIdentifiers>win-x64;win-arm64</RuntimeIdentifiers>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.260101001" />
  </ItemGroup>
</Project>
```

## Why These Settings

- Windows widgets depend on Windows-only APIs, so the TFM must be Windows-specific.
- Runtime identifiers are needed for predictable local build/deploy outputs.
- Widget provider APIs are available through the Windows App SDK package.

## Step 02 Extra (Advanced, Optional)

If you want to reduce full Windows App SDK usage outside Debug, you can use a conditional package pattern (similar to the Witals sample):

```xml
<ItemGroup Condition="'$(Configuration)' == 'Debug'">
  <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.260101001" />
  <PackageReference Include="Microsoft.WindowsAppSDK.Widgets" Version="1.8.251231004" />
</ItemGroup>

<ItemGroup Condition="'$(Configuration)' != 'Debug'">
  <PackageReference Include="Microsoft.WindowsAppSDK.Widgets" Version="1.8.251231004" />
</ItemGroup>
```

Keep this pattern out of the base tutorial flow to avoid adding complexity too early.

## Image Placeholders

- `[IMAGE: vs-new-project.png]` - Base project creation in Visual Studio.
- `[IMAGE: project-properties-target-os.png]` - Target OS and framework settings.

## Verify Step 02

Run:

```powershell
dotnet build Xakpc.Widgets.Playground.sln
```

Expected result: successful build with updated project metadata.

## Common Mistakes

- Forgetting the Windows TFM and trying to use widget APIs from plain `net10.0`.
- Adding advanced project structure too early.
- Adding packaging complexity in this step (packaging comes later).

## Next Step

Step 03 adds the Adaptive Card templates (`CatFactTemplate.json` and `LoadingTemplate.json`).
