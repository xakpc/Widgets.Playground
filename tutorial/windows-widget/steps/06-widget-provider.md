# Step 06 - Implement the Widget Provider Lifecycle

This step adds the provider class that the Windows widget host calls at runtime.

## Goal

After this step, the project should have:

- a `WidgetProvider` with required lifecycle callbacks
- startup restoration of already-pinned widgets
- fire-and-forget refresh flow from synchronous callbacks
- update helpers for loading/main card rendering
- data payload support for `backgroundImageDataUri`

## Scope

This step adds provider lifecycle logic only.
COM factory and app entrypoint wiring come in the next steps.

## File Added

- `src/Xakpc.Widgets.Playground/WidgetProvider.cs`

## Lifecycle Methods Implemented

- `CreateWidget(WidgetContext widgetContext)`
- `DeleteWidget(string widgetId, string customState)`
- `OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)`
- `OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)`
- `Activate(WidgetContext widgetContext)`
- `Deactivate(string widgetId)`

## Provider Skeleton (Copy/Paste)

```csharp
using Microsoft.Windows.Widgets.Providers;
using Xakpc.Widgets.Playground.Models;
using Xakpc.Widgets.Playground.Services;

namespace Xakpc.Widgets.Playground;

internal sealed class WidgetProvider : IWidgetProvider
{
    private readonly Dictionary<string, CompactWidgetInfo> _runningWidgets = new(StringComparer.Ordinal);
    private readonly CatFactService _catFacts = new();

    public WidgetProvider()
    {
        RecoverRunningWidgets();
    }

    public void CreateWidget(WidgetContext widgetContext) { /* ... */ }
    public void DeleteWidget(string widgetId, string customState) { /* ... */ }
    public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs) { /* ... */ }
    public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs) { /* ... */ }
    public void Activate(WidgetContext widgetContext) { /* ... */ }
    public void Deactivate(string widgetId) { /* ... */ }
}
```

## Core Behavior

### CreateWidget

- create/reuse `CompactWidgetInfo` for the new widget id
- send loading template immediately
- start async refresh (`_ = RefreshAndUpdateAsync(widgetId)`)

### OnActionInvoked

- handle only `refresh` verb
- trigger async refresh for that widget id
- in current templates, refresh action is visible on non-small sizes (`medium`/`large`)

### OnWidgetContextChanged

- re-send current state through `UpdateWidget`
- no API call needed for size change

### Activate / Deactivate

- `Activate` re-renders current state and triggers a background refresh
- `Deactivate` stays minimal in this tutorial (no polling system yet)

### Startup restoration

- constructor calls `RecoverRunningWidgets()`
- reads existing pinned widgets from `WidgetManager.GetDefault().GetWidgetInfos()`
- restores `CustomState` into in-memory dictionary

## Provider Deep Dive (What Happens Internally)

This section explains the runtime flow in `WidgetProvider.cs` in detail.

### 1. In-memory state and synchronization

The provider keeps an in-memory map of pinned widgets:

```csharp
private readonly Dictionary<string, CompactWidgetInfo> _runningWidgets = new(StringComparer.Ordinal);
```

Key details:

- key: host-assigned widget instance id (`WidgetId`)
- value: minimal state model (`DefinitionId`, `CustomState`)
- all access is protected by `_sync` because:
  - host callbacks are synchronous entry points
  - refresh work runs asynchronously in background tasks

Without the lock, callbacks and refresh tasks could race and corrupt state.

### 2. Startup path: recovering persisted state

`WidgetProvider` constructor calls `RecoverRunningWidgets()` immediately.

`RecoverRunningWidgets()`:

1. calls `WidgetManager.GetDefault().GetWidgetInfos()`
2. iterates existing pinned widgets
3. rebuilds `_runningWidgets` from host data
4. restores `CustomState` (last persisted fact) or uses default placeholder

Why this matters:

- widgets may already be pinned when provider process starts
- users should see last known fact immediately after restart

### 3. Create flow: new widget pin

When user pins the widget, host calls `CreateWidget(...)`.

Flow:

1. `GetOrCreateWidget(...)` creates/returns widget state entry
2. `SendLoadingWidget(...)` pushes loading template immediately
3. `_ = RefreshAndUpdateAsync(widgetId)` starts async fact fetch

Reasoning:

- callback must return quickly
- loading UI prevents blank widget while network call runs

### 4. Action flow: refresh button

Template action sends `verb = "refresh"`.

`OnActionInvoked(...)`:

1. ignores verbs other than `refresh`
2. validates widget still exists (could be unpinned)
3. starts background refresh task

No blocking call is done directly inside callback.

### 5. Context-change flow: resize/theme/layout

Host calls `OnWidgetContextChanged(...)` on context changes.

Provider behavior:

- read current widget snapshot
- call `SendFactWidget(snapshot, null)` with existing data

No API refetch here; same fact is re-rendered under new host context.

### 6. Activate/deactivate flow

`Activate(...)`:

1. ensures widget state exists
2. sends current fact immediately
3. always triggers background refresh so non-medium sizes still refresh on activation

`Deactivate(...)`:

- intentionally no-op in this tutorial
- no polling infrastructure exists yet, so nothing to pause

### 7. Async refresh pipeline

`RefreshAndUpdateAsync(widgetId)`:

1. calls `CatFactService.GetFactAsync()`
2. maps null/failure to lightweight error message
3. re-enters lock and checks widget still exists
4. updates `CustomState` only when a valid fact is fetched
5. sends updated card with current fact + optional error text

Important race handling:

- if widget is deleted during fetch, method returns safely
- stale background result cannot recreate deleted widget

### 8. Rendering and persistence contract

`SendLoadingWidget(...)` sends:

- `Template = LoadingTemplate`
- `Data = "{}"`
- `CustomState = widget.CustomState`

`SendFactWidget(...)` sends:

- `Template = MainTemplate`
- `Data = BuildDataPayload(...)` with `fact`, `errorMessage`, and `backgroundImageDataUri`
- `CustomState = fact`

`CustomState` is critical:

- host persists this string
- `RecoverRunningWidgets()` reads it back on process restart

### 9. Why `EscapeJson` exists

Widget data is passed as a raw JSON string.

If fact text contains quotes or slashes and is not escaped, payload becomes invalid JSON and rendering can fail.
`EscapeJson(...)` prevents that by escaping `\` and `"` before payload assembly.

### 10. Background image data flow

Current card background is data-driven:

1. provider loads `Assets/background-small.png` once
2. provider converts bytes to `data:image/png;base64,...`
3. payload includes `backgroundImageDataUri`
4. template binds `backgroundImage` to `${backgroundImageDataUri}`

This avoids relying on `ms-appx:///` URI resolution inside widget background rendering.

## Update Helper Snippet (Copy/Paste)

```csharp
private void SendFactWidget(CompactWidgetInfo widget, string? errorMessage)
{
    var fact = string.IsNullOrWhiteSpace(widget.CustomState)
        ? "Loading your first cat fact..."
        : widget.CustomState;

    var update = new WidgetUpdateRequestOptions(widget.WidgetId)
    {
        Template = MainTemplate.Value,
        Data = BuildDataPayload(fact, errorMessage, BackgroundImageDataUri.Value),
        CustomState = fact
    };

    WidgetManager.GetDefault().UpdateWidget(update);
}
```

## Background Data Payload Snippet (Copy/Paste)

```csharp
private static string BuildDataPayload(string fact, string? errorMessage, string backgroundImageDataUri)
{
    if (string.IsNullOrWhiteSpace(errorMessage))
    {
        return $$"""{"fact":"{{EscapeJson(fact)}}","errorMessage":null,"backgroundImageDataUri":"{{EscapeJson(backgroundImageDataUri)}}"}""";
    }

    return $$"""{"fact":"{{EscapeJson(fact)}}","errorMessage":"{{EscapeJson(errorMessage)}}","backgroundImageDataUri":"{{EscapeJson(backgroundImageDataUri)}}"}""";
}
```

## Image Data URI Helper Snippet (Copy/Paste)

```csharp
private static string LoadImageAsDataUri(string relativePath, string mimeType)
{
    var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
    var fullPath = Path.Combine(AppContext.BaseDirectory, normalizedPath);
    var imageBytes = File.ReadAllBytes(fullPath);
    return $"data:{mimeType};base64,{Convert.ToBase64String(imageBytes)}";
}
```

## JSON Escaping Helper (Copy/Paste)

```csharp
private static string EscapeJson(string value) =>
    value.Replace("\\", "\\\\").Replace("\"", "\\\"");
```

This avoids broken widget data payloads when facts contain quotes or slashes.

## Async Refresh Pattern

`IWidgetProvider` callbacks are synchronous, so API work is done with fire-and-forget tasks:

```csharp
_ = RefreshAndUpdateAsync(widgetId);
```

`RefreshAndUpdateAsync` fetches data, updates `CustomState`, then pushes `UpdateWidget`.

## Verify Step 06

Run:

```powershell
dotnet build Xakpc.Widgets.Playground.sln
```

Expected result: successful build with provider lifecycle class compiled.

## Common Mistakes

- awaiting network calls directly inside synchronous callbacks.
- forgetting to set `CustomState` when updating widget content.
- skipping startup recovery, which causes blank state after app restart.
- forgetting to include `backgroundImageDataUri` in payload while template expects it.

## Next Step

Step 07 adds COM class factory boilerplate (`FactoryHelper.cs`).
