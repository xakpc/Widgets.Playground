# Step 05 - Add the Widget State Model

This step adds the minimal per-widget state object used by the provider.

## Goal

After this step, the project should have:

- one compact model for widget instance state
- a clear place to store host widget identifiers
- a `CustomState` field for persisted data (last cat fact)

## Scope

This step adds the model only.
Provider lifecycle integration comes in Step 06.

## File Added

- `src/Xakpc.Widgets.Playground/Models/CompactWidgetInfo.cs`

## Model Snippet (Copy/Paste)

```csharp
namespace Xakpc.Widgets.Playground.Models;

public sealed class CompactWidgetInfo
{
    public string WidgetId { get; set; } = string.Empty;
    public string DefinitionId { get; set; } = string.Empty;
    public string CustomState { get; set; } = string.Empty;
}
```

## What Each Field Means

- `WidgetId`:
  - runtime instance id assigned by the host
  - used as dictionary key in provider code
- `DefinitionId`:
  - widget definition name from manifest (for this tutorial: one cat widget definition)
  - useful for validation/logging and future multi-definition evolution
- `CustomState`:
  - host-persisted string payload
  - stores the last known cat fact so content survives restart

## How Persistence Works

In Step 06, provider update calls will set:

- `WidgetUpdateRequestOptions.CustomState = model.CustomState`

On activation/recovery, host returns the same value through widget info.
This gives persistence without adding local files or a database.

## Practical Usage Snippet (Next Step Context)

```csharp
var state = new CompactWidgetInfo
{
    WidgetId = widgetContext.Id,
    DefinitionId = widgetContext.DefinitionId,
    CustomState = "Loading your first cat fact..."
};

runningWidgets[state.WidgetId] = state;
```

## Verify Step 05

Run:

```powershell
dotnet build Xakpc.Widgets.Playground.sln
```

Expected result: successful build with model class compiled.

## Common Mistakes

- Mixing `WidgetId` and `DefinitionId`.
- Treating `CustomState` as transient only (it is persistence input/output).
- Over-modeling state too early for a single-widget tutorial.

## Next Step

Step 06 wires provider lifecycle callbacks and uses this model for runtime + persisted state.
