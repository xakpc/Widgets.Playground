# Step 04 - Build the Cat Fact Service

This step adds a small service class that fetches data from `catfact.ninja`.

## Goal

After this step, the project should have:

- a dedicated service for cat fact API calls
- one shared `HttpClient` instance
- safe JSON parsing
- `null` fallback on recoverable failures

## Scope

This step adds service code only.
Provider lifecycle wiring comes later.

## File Added

- `src/Xakpc.Widgets.Playground/Services/CatFactService.cs`

## Service Snippet (Copy/Paste)

```csharp
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Xakpc.Widgets.Playground.Services;

public sealed class CatFactService
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://catfact.ninja/"),
        Timeout = TimeSpan.FromSeconds(10)
    };

    public async Task<string?> GetFactAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await Http.GetFromJsonAsync<CatFactResponse>("fact", ct);
            var fact = response?.Fact?.Trim();
            return string.IsNullOrWhiteSpace(fact) ? null : fact;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private sealed class CatFactResponse
    {
        [JsonPropertyName("fact")]
        public string? Fact { get; init; }
    }
}
```

## Why This Design

- One static `HttpClient` avoids connection churn and socket pressure.
- `GetFactAsync` returns `string?` so caller can decide fallback behavior.
- Timeout is set in service to keep API stalls from hanging UI updates.
- Cancellation still propagates (`OperationCanceledException` is rethrown).

## API Shape Used

Request:
- `GET https://catfact.ninja/fact`

Expected response shape:

```json
{
  "fact": "Cats can rotate their ears 180 degrees.",
  "length": 44
}
```

This tutorial only needs the `fact` field, so the JSON snippet above is enough and no screenshot is required.

## Verify Step 04

Run:

```powershell
dotnet build Xakpc.Widgets.Playground.sln
```

Expected result: successful build with service class compiled.

## Common Mistakes

- Creating new `HttpClient` per request.
- Swallowing cancellation instead of rethrowing.
- Treating empty/whitespace `fact` as valid content.

## Next Step

Step 05 adds the minimal widget state model (`CompactWidgetInfo`).
