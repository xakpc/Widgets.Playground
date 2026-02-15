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

        // Aggregation is not supported for this local server.
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
