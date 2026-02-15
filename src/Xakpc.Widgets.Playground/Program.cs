using System.Runtime.InteropServices;
using Xakpc.Widgets.Playground.Com;

namespace Xakpc.Widgets.Playground;

internal static class Program
{
    // COM class registration flags for an out-of-proc local server.
    private const uint CLSCTX_LOCAL_SERVER = 0x4;
    private const uint REGCLS_MULTIPLEUSE = 0x1;

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("ole32.dll")]
    private static extern int CoRegisterClassObject(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
        [MarshalAs(UnmanagedType.Interface)] IClassFactory pUnk,
        uint dwClsContext,
        uint flags,
        out uint lpdwRegister);

    [DllImport("ole32.dll")]
    private static extern int CoRevokeClassObject(uint dwRegister);

    [MTAThread]
    private static void Main(string[] args)
    {
        // Widget host launches the provider with this argument from manifest COM activation.
        if (args.Length == 0 || !string.Equals(args[0], "-RegisterProcessAsComServer", StringComparison.Ordinal))
        {
            Console.WriteLine("Not launched for widget provider activation. Exiting.");
            return;
        }

        // Required so WinRT COM projections can marshal managed provider objects.
        WinRT.ComWrappersSupport.InitializeComWrappers();

        uint cookie = 0;
        // Factory creates WidgetProvider instances when host requests this CLSID.
        var factory = new WidgetProviderFactory<WidgetProvider>();
        var registerResult = CoRegisterClassObject(
            typeof(WidgetProvider).GUID,
            factory,
            CLSCTX_LOCAL_SERVER,
            REGCLS_MULTIPLEUSE,
            out cookie);

        if (registerResult < 0)
        {
            Marshal.ThrowExceptionForHR(registerResult);
        }

        try
        {
            // Console path is for developer debugging.
            if (GetConsoleWindow() != IntPtr.Zero)
            {
                Console.WriteLine("Widget provider registered. Press ENTER to exit.");
                Console.ReadLine();
            }
            else
            {
                // Packaged/no-console path: stay alive while at least one widget exists.
                WidgetProvider.GetEmptyWidgetListEvent().WaitOne();
            }
        }
        finally
        {
            // Always revoke class registration when process exits.
            if (cookie != 0)
            {
                CoRevokeClassObject(cookie);
            }
        }
    }
}
