# Step 07 - Add COM Class Factory Boilerplate

This step adds the COM factory plumbing required for widget host activation.

## Goal

After this step, the project should have:

- a minimal COM `IClassFactory` definition
- a generic `WidgetProviderFactory<TProvider>` that creates provider instances
- no registration logic yet (that comes in Step 08)

## Scope

This step defines COM factory types only.
Process registration and lifetime management are handled in the next step.

## File Added

- `src/Xakpc.Widgets.Playground/FactoryHelper.cs`

## Why This Is Needed

Windows widgets use COM activation:

1. host asks for a CLSID from manifest mapping
2. app process provides a class factory for that CLSID
3. factory creates `IWidgetProvider` object instances

Without a class factory, the host cannot create your provider.

## Factory Snippet (Copy/Paste)

```csharp
using Microsoft.Windows.Widgets.Providers;
using System.Runtime.InteropServices;
using WinRT;

namespace Xakpc.Widgets.Playground.Com;

internal static class ComGuids
{
    public const string IClassFactory = "00000001-0000-0000-C000-000000000046";
    public const string IUnknown = "00000000-0000-0000-C000-000000000046";
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid(ComGuids.IClassFactory)]
internal interface IClassFactory
{
    [PreserveSig]
    int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);

    [PreserveSig]
    int LockServer(bool fLock);
}

internal sealed class WidgetProviderFactory<TProvider> : IClassFactory
    where TProvider : IWidgetProvider, new()
{
    private static readonly Guid IUnknownGuid = Guid.Parse(ComGuids.IUnknown);
    private static readonly Guid WidgetProviderInterfaceGuid = typeof(IWidgetProvider).GUID;

    private const int S_OK = 0;
    private const int CLASS_E_NOAGGREGATION = unchecked((int)0x80040110);
    private const int E_NOINTERFACE = unchecked((int)0x80004002);

    public int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
    {
        ppvObject = IntPtr.Zero;

        if (pUnkOuter != IntPtr.Zero)
        {
            return CLASS_E_NOAGGREGATION;
        }

        var classGuid = typeof(TProvider).GUID;
        if (riid != classGuid && riid != WidgetProviderInterfaceGuid && riid != IUnknownGuid)
        {
            return E_NOINTERFACE;
        }

        ppvObject = MarshalInspectable<IWidgetProvider>.FromManaged(new TProvider());
        return S_OK;
    }

    public int LockServer(bool fLock) => S_OK;
}
```

## Method Responsibilities

- `CreateInstance`:
  - validates aggregation rules
  - validates requested interface GUID
  - creates managed provider and exposes COM pointer
- `LockServer`:
  - returns success for this simple tutorial implementation

## Image Placeholders

- `[IMAGE: com-activation-path.png]` - Host request -> class factory -> provider instance flow.

## Verify Step 07

Run:

```powershell
dotnet build Xakpc.Widgets.Playground.sln
```

Expected result: successful build with COM factory types compiled.

## Common Mistakes

- using wrong COM GUID constants for `IClassFactory` or `IUnknown`
- throwing exceptions instead of returning COM `HRESULT`s
- trying to add registration logic here (save that for Step 08)

## Next Step

Step 08 wires `Program.cs` to register and revoke the COM class object.
