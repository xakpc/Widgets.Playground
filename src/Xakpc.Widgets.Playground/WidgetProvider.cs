using Microsoft.Windows.Widgets.Providers;
using System.Runtime.InteropServices;
using Xakpc.Widgets.Playground.Models;
using Xakpc.Widgets.Playground.Services;

namespace Xakpc.Widgets.Playground;

[ComVisible(true)]
[ComDefaultInterface(typeof(IWidgetProvider))]
[Guid("4C363B3C-0E2D-4ACF-A797-40DE1B6D4FAD")] // Source of truth for COM registration; manifest must use the same CLSID.
internal sealed class WidgetProvider : IWidgetProvider
{
    private const string DefaultFact = "Loading your first cat fact...";
    private const string RefreshError = "Could not refresh right now. Showing last saved fact.";

    // Host callbacks and background refresh can run concurrently.
    private readonly Lock _sync = new();
    private readonly Dictionary<string, CompactWidgetInfo> _runningWidgets = new(StringComparer.Ordinal);
    private readonly CatFactService _catFacts = new();
    // Signals Program.cs when the last widget is removed so process can exit.
    private static readonly ManualResetEvent EmptyWidgetListEvent = new(false);

    // Load template files once from packaged output.
    private static readonly Lazy<string> MainTemplate = new(() => LoadTemplate("Templates/CatFactTemplate.json"));
    private static readonly Lazy<string> LoadingTemplate = new(() => LoadTemplate("Templates/LoadingTemplate.json"));
    // Background image as a data URI because widget card background does not reliably resolve ms-appx URIs.
    private static readonly Lazy<string> BackgroundImageDataUri = new(() => LoadImageAsDataUri("Assets/background-small.png", "image/png"));

    public WidgetProvider()
    {
        RecoverRunningWidgets();
    }

    public void CreateWidget(WidgetContext widgetContext)
    {
        var widget = GetOrCreateWidget(widgetContext, null);
        SendLoadingWidget(widget);
        // IWidgetProvider methods are sync; refresh runs in background.
        _ = RefreshAndUpdateAsync(widget.WidgetId);
    }

    public void DeleteWidget(string widgetId, string customState)
    {
        lock (_sync)
        {
            _runningWidgets.Remove(widgetId);

            // No widgets left: allow non-console process lifetime wait to complete.
            if (_runningWidgets.Count == 0)
            {
                EmptyWidgetListEvent.Set();
            }
        }
    }

    public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        if (!string.Equals(actionInvokedArgs.Verb, "refresh", StringComparison.Ordinal))
        {
            return;
        }

        if (!HasWidget(actionInvokedArgs.WidgetContext.Id))
        {
            return;
        }

        _ = RefreshAndUpdateAsync(actionInvokedArgs.WidgetContext.Id);
    }

    public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        var widget = GetWidgetSnapshot(contextChangedArgs.WidgetContext.Id);
        if (widget is null)
        {
            return;
        }

        // Re-render current data for size/theme/layout changes.
        SendFactWidget(widget, null);
    }

    public void Activate(WidgetContext widgetContext)
    {
        var widget = GetOrCreateWidget(widgetContext, null);
        SendFactWidget(widget, null);
        _ = RefreshAndUpdateAsync(widget.WidgetId);
    }

    public void Deactivate(string widgetId)
    {
        // Intentionally minimal for tutorial: no active polling to pause.
    }

    // Program.cs waits on this in non-console mode.
    public static WaitHandle GetEmptyWidgetListEvent() => EmptyWidgetListEvent;

    private CompactWidgetInfo GetOrCreateWidget(WidgetContext context, string? customState)
    {
        lock (_sync)
        {
            if (_runningWidgets.TryGetValue(context.Id, out var existing))
            {
                if (!string.IsNullOrWhiteSpace(customState))
                {
                    existing.CustomState = customState;
                }

                return Clone(existing);
            }

            var created = new CompactWidgetInfo
            {
                WidgetId = context.Id,
                DefinitionId = context.DefinitionId,
                CustomState = string.IsNullOrWhiteSpace(customState) ? DefaultFact : customState
            };

            _runningWidgets[created.WidgetId] = created;
            // At least one active widget exists, keep provider process alive.
            EmptyWidgetListEvent.Reset();
            return Clone(created);
        }
    }

    private bool HasWidget(string widgetId)
    {
        lock (_sync)
        {
            return _runningWidgets.ContainsKey(widgetId);
        }
    }

    private CompactWidgetInfo? GetWidgetSnapshot(string widgetId)
    {
        lock (_sync)
        {
            return _runningWidgets.TryGetValue(widgetId, out var widget) ? Clone(widget) : null;
        }
    }

    private async Task RefreshAndUpdateAsync(string widgetId)
    {
        string? fetchedFact = null;
        string? errorMessage = null;

        try
        {
            fetchedFact = await _catFacts.GetFactAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(fetchedFact))
            {
                errorMessage = RefreshError;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Keep last known fact and surface a lightweight refresh error.
            errorMessage = RefreshError;
        }

        CompactWidgetInfo? snapshot;
        lock (_sync)
        {
            if (!_runningWidgets.TryGetValue(widgetId, out var widget))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(fetchedFact))
            {
                widget.CustomState = fetchedFact;
            }

            snapshot = Clone(widget);
        }

        SendFactWidget(snapshot, errorMessage);
    }

    private void SendLoadingWidget(CompactWidgetInfo widget)
    {
        var update = new WidgetUpdateRequestOptions(widget.WidgetId)
        {
            Template = LoadingTemplate.Value,
            Data = "{}",
            CustomState = widget.CustomState
        };

        WidgetManager.GetDefault().UpdateWidget(update);
    }

    private void SendFactWidget(CompactWidgetInfo widget, string? errorMessage)
    {
        var fact = string.IsNullOrWhiteSpace(widget.CustomState) ? DefaultFact : widget.CustomState;
        var update = new WidgetUpdateRequestOptions(widget.WidgetId)
        {
            Template = MainTemplate.Value,
            Data = BuildDataPayload(fact, errorMessage, BackgroundImageDataUri.Value),
            CustomState = fact
        };

        WidgetManager.GetDefault().UpdateWidget(update);
    }

    private void RecoverRunningWidgets()
    {
        try
        {
            // Rehydrate in-memory state from host-persisted CustomState.
            var infos = WidgetManager.GetDefault().GetWidgetInfos();
            if (infos is null)
            {
                return;
            }

            lock (_sync)
            {
                foreach (var info in infos)
                {
                    var context = info.WidgetContext;
                    _runningWidgets[context.Id] = new CompactWidgetInfo
                    {
                        WidgetId = context.Id,
                        DefinitionId = context.DefinitionId,
                        CustomState = string.IsNullOrWhiteSpace(info.CustomState) ? DefaultFact : info.CustomState
                    };
                }

                if (_runningWidgets.Count == 0)
                {
                    // If app starts with no pinned widgets, no wait is needed.
                    EmptyWidgetListEvent.Set();
                }
                else
                {
                    // Existing pinned widgets require process to remain available.
                    EmptyWidgetListEvent.Reset();
                }
            }
        }
        catch
        {
            // WidgetManager can fail before proper widget host activation.
        }
    }

    private static string BuildDataPayload(string fact, string? errorMessage, string backgroundImageDataUri)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return $$"""{"fact":"{{EscapeJson(fact)}}","errorMessage":null,"backgroundImageDataUri":"{{EscapeJson(backgroundImageDataUri)}}"}""";
        }

        return $$"""{"fact":"{{EscapeJson(fact)}}","errorMessage":"{{EscapeJson(errorMessage)}}","backgroundImageDataUri":"{{EscapeJson(backgroundImageDataUri)}}"}""";
    }

    private static string EscapeJson(string value) =>
        // Widget data is passed as raw JSON string.
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string LoadTemplate(string relativePath)
    {
        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(AppContext.BaseDirectory, normalizedPath);
        return File.ReadAllText(fullPath);
    }

    private static string LoadImageAsDataUri(string relativePath, string mimeType)
    {
        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(AppContext.BaseDirectory, normalizedPath);
        var imageBytes = File.ReadAllBytes(fullPath);
        return $"data:{mimeType};base64,{Convert.ToBase64String(imageBytes)}";
    }

    private static CompactWidgetInfo Clone(CompactWidgetInfo widget) =>
        new()
        {
            WidgetId = widget.WidgetId,
            DefinitionId = widget.DefinitionId,
            CustomState = widget.CustomState
        };
}
