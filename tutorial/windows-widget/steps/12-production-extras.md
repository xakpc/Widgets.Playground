# Step 12 - Production Extras (Lightweight)

This step adds practical hardening ideas without changing the tutorial architecture.

## Goal

After this step, you should know how to:

- improve observability with minimal logging
- reduce transient API failures with simple retry logic
- avoid noisy refresh behavior
- keep widget output safe and stable under edge cases

## Scope

Guidance and optional snippets only.
No framework-heavy refactor, DI container, or multi-layer architecture is introduced.

## File Added

- `tutorial/windows-widget/steps/12-production-extras.md`

## What The Current Tutorial Already Does Well

The baseline implementation already includes:

- API timeout in `CatFactService` (`10s`)
- null/failure fallback behavior in service
- persisted last known fact via `CustomState`
- defensive update path when refresh fails
- JSON escaping for payload safety

Step 12 builds on top of that baseline.

## Extra 1 - Minimal Logging (No New Package Required)

For lightweight diagnostics, add a tiny helper and use it in key paths.

Suggested log points:

- `CreateWidget`, `DeleteWidget`
- `OnActionInvoked` with verb
- `Activate`/`Deactivate`
- refresh success/failure in `RefreshAndUpdateAsync`
- startup recovery count in `RecoverRunningWidgets`

Example helper:

```csharp
internal static class Log
{
    public static void Info(string message)
    {
        var line = $"[{DateTimeOffset.Now:O}] INFO {message}";
        System.Diagnostics.Debug.WriteLine(line);
        Console.WriteLine(line);
    }

    public static void Error(string message, Exception ex)
    {
        var line = $"[{DateTimeOffset.Now:O}] ERROR {message}: {ex.GetType().Name} {ex.Message}";
        System.Diagnostics.Debug.WriteLine(line);
        Console.WriteLine(line);
    }
}
```

Keep logs short and high-signal. Do not log raw secrets or tokens.

## Extra 2 - Add One Simple Retry For Transient API Failures

Current behavior is already safe on failure. For better success rate on flaky networks, add one delayed retry.

Example shape:

```csharp
public async Task<string?> GetFactAsync(CancellationToken ct = default)
{
    for (var attempt = 0; attempt < 2; attempt++)
    {
        try
        {
            var response = await Http.GetFromJsonAsync<CatFactResponse>("fact", ct);
            var fact = response?.Fact?.Trim();
            if (!string.IsNullOrWhiteSpace(fact))
            {
                return fact;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException) when (attempt == 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(250), ct);
        }
        catch (JsonException) when (attempt == 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(250), ct);
        }
        catch
        {
            return null;
        }
    }

    return null;
}
```

Keep retries conservative. Widgets should stay responsive.

## Extra 3 - Add Refresh Throttling

Activation can occur frequently. Optional throttling avoids unnecessary API calls.

Lightweight approach:

- track last refresh time per widget id
- skip refresh if last refresh was very recent (for example, within 10 seconds)

This prevents repeated calls during rapid open/close interactions.

## Extra 4 - Bound Payload Size Defensively

Some APIs can return unexpectedly long text.
Consider truncating to a safe display length before writing payload/custom state.

Example:

```csharp
private static string NormalizeFact(string fact)
{
    const int max = 400;
    var trimmed = fact.Trim();
    if (trimmed.Length <= max)
    {
        return trimmed;
    }

    return $"{trimmed[..max]}...";
}
```

This prevents oversized content from degrading small layouts.

## Extra 5 - Keep Exception Boundaries Explicit

In provider callbacks, never let unexpected exceptions bubble into host callbacks.
Catch, log, and continue with fallback content where possible.

High-value boundaries:

- lifecycle callbacks
- refresh/update pipeline
- startup recovery

## Production-Ready Checklist (Minimal)

1. Logging exists for lifecycle + refresh events.
2. At least one transient retry is present and bounded.
3. Refresh throttling prevents accidental bursts.
4. Payload size is normalized.
5. Failure paths preserve last good fact and keep widget usable.

## Verify Step 12

Run:

```powershell
dotnet build Xakpc.Widgets.Playground.sln
```

Expected result: successful build (if optional snippets are implemented).

## Common Mistakes

- adding heavy abstractions that obscure the tutorial flow
- retrying too aggressively and causing noisy network usage
- logging too much low-value data
- dropping fallback behavior while adding new resilience logic

## Next Step

Step 13 finalizes tutorial index, navigation, glossary, and learning progression.
