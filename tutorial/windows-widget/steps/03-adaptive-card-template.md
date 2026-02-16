# Step 03 - Add Adaptive Card Templates

This step adds the two card templates used by the widget UI:

- main content card (`CatFactTemplate.json`)
- loading card (`LoadingTemplate.json`)

## Goal

After this step, the project should include:

- a reusable main template with data binding, background image binding, and a refresh action
- a lightweight loading template for startup and refresh transitions
- template files tracked as project content assets

## Scope

This step adds template assets only.
No provider lifecycle code is implemented yet.

## Adaptive Cards (Quick Explanation)

Adaptive Cards are declarative JSON UI definitions. You describe the card in JSON, and the host renders it.

- Structure:
  - root card object: `type`, `version`, optional `$schema`
  - `body`: visual elements (`TextBlock`, `Image`, `Container`, etc.)
  - `actions`: user actions (`Action.Execute`, `Action.OpenUrl`, etc.)
  - optional binding/conditions: `${...}` values and `$when` conditions
- Runtime flow:
  1. provider sends template JSON + data
  2. host resolves bindings and renders the card
  3. user action triggers an action verb back to the provider
  4. provider returns updated content, host re-renders

Reference:
- https://learn.microsoft.com/en-us/adaptive-cards/
- https://learn.microsoft.com/en-us/adaptive-cards/schema-explorer/adaptive-card

## Background Image Size Requirements

For this tutorial, use a small-widget background image with these requirements:

- target size: `300x200` pixels
- aspect ratio: `3:2`
- source asset path in project: `Assets/background-small.png`
- keep the visual focus near the center so text remains readable with overlay

## How Background Rendering Works

For widget templates, background visuals can come from `backgroundImage` on the root `AdaptiveCard` (or on a container).

- `backgroundImage` supports URL/data URL image sources.
- in this tutorial, use a base64 data URL (`data:image/png;base64,...`) provided via template data
- common formats: PNG, JPEG, GIF.
- with `fillMode: "cover"`, the image fills available space and may crop.
- `horizontalAlignment` / `verticalAlignment` control crop position when cropping is needed.

Reference:
- https://learn.microsoft.com/en-us/adaptive-cards/schema-explorer/background-image

## Dark/Light Scheme In Widgets

Widgets expose host properties through `$host.*`. For theme:

- `$host.hostTheme` is `"light"` or `"dark"`.
- use `$when` conditions to show theme-specific elements if needed.

Example:

```json
{
  "type": "TextBlock",
  "text": "Dark theme hint",
  "$when": "${$host.hostTheme==\"dark\"}"
}
```

In this tutorial we use one background image and a compact text layout so the visual background stays visible.

Reference:
- https://learn.microsoft.com/en-us/windows/apps/design/widgets/widgets-create-a-template

## Adaptive Card Version: What It Means

`version` declares the minimum schema version required by your card.

- if host support is lower than this value, host can render `fallbackText` instead.
- top-level cards should declare `version`.
- choose the lowest version that supports all features you use.

Quick guidance for this tutorial:

- `1.0`: core card structure (`type`, `body`, `actions`)
- `1.2`: background image support on card/container
- `1.4`: `Action.Execute` support in Adaptive Card schema
- `1.5`: safe modern baseline used in our templates

Reference:
- https://learn.microsoft.com/en-us/adaptive-cards/schema-explorer/adaptive-card

## Files Added

- `src/Xakpc.Widgets.Playground/Templates/CatFactTemplate.json`
- `src/Xakpc.Widgets.Playground/Templates/LoadingTemplate.json`
- `src/Xakpc.Widgets.Playground/Assets/background-small.png`

## Project File Update

Add template content entries to `src/Xakpc.Widgets.Playground/Xakpc.Widgets.Playground.csproj`:

```xml
<ItemGroup>
  <Content Include="Templates\CatFactTemplate.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  <Content Include="Templates\LoadingTemplate.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  <Content Include="Assets\background-small.png">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

## Main Template Snippet (Copy/Paste)

`CatFactTemplate.json`:

```json
{
  "type": "AdaptiveCard",
  "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
  "version": "1.5",
  "backgroundImage": "${backgroundImageDataUri}",
  "header": {
    "text": "Random Cat Fact"
  },
  "body": [
    {
      "type": "Container",
      "items": [
        {
          "type": "TextBlock",
          "text": "${fact}",
          "horizontalAlignment": "Center",
          "weight": "Bolder",
          "wrap": true,
          "$when": "${$host.widgetSize == \"small\"}"
        },
        {
          "type": "TextBlock",
          "text": "${fact}",
          "horizontalAlignment": "Center",
          "weight": "Bolder",
          "wrap": true,
          "size": "Large",
          "$when": "${$host.widgetSize != \"small\"}"
        },
        {
          "type": "TextBlock",
          "text": "${errorMessage}",
          "wrap": true,
          "color": "Warning",
          "isSubtle": true,
          "size": "Small",
          "$when": "${errorMessage != null}"
        }
      ],
      "height": "stretch",
      "horizontalAlignment": "Center",
      "verticalContentAlignment": "Center"
    }
  ],
  "actions": [
    {
      "type": "Action.Execute",
      "title": "Refresh",
      "verb": "refresh",
      "$when": "${$host.widgetSize != \"small\"}"
    }
  ]
}
```

## Loading Template Snippet (Copy/Paste)

`LoadingTemplate.json`:

```json
{
  "type": "AdaptiveCard",
  "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
  "version": "1.5",
  "body": [
    {
      "type": "TextBlock",
      "text": "Loading latest fact...",
      "isSubtle": true,
      "wrap": true,
      "height": "stretch"
    }
  ]
}
```

## Why This Template Shape

- `${fact}` keeps data binding explicit and simple for beginners.
- `backgroundImageDataUri` keeps card background reliable in widget host rendering.
- one text layout for `small` and one for non-`small` keeps small readable and medium/large punchier.
- `refresh` action is hidden on `small`; `medium` and `large` show the button.
- `$when` on `errorMessage` introduces conditional UI without adding complexity.
- separate loading template avoids blank UI during async work.

## Image Placeholders

- `[IMAGE: adaptive-card-designer-preview.png]` - Template preview in Adaptive Cards Designer.

## Verify Step 03

Run:

```powershell
dotnet build Xakpc.Widgets.Playground.sln
```

Expected result: successful build with template assets included.

## Common Mistakes

- Using invalid Adaptive Card schema version.
- Misspelling binding names (`fact`, `errorMessage`, `backgroundImageDataUri`).
- Forgetting to include template files as content assets.

## Next Step

Step 04 adds `CatFactService.cs` for API calls and fallback behavior.
