# Step 08 - Wire the Program Entry Point

This step connects your COM factory to process startup so Windows can activate the provider.

## Goal

After this step, the app should:

- register a COM class object when launched for widget activation
- use `WidgetProvider` COM attribute GUID as the runtime registration source
- keep process alive while widgets exist
- revoke COM registration on exit

## Scope

This step wires startup and process lifetime.
Manifest declarations come in Step 09.

## Files Updated

- `src/Xakpc.Widgets.Playground/Program.cs`
- `src/Xakpc.Widgets.Playground/WidgetProvider.cs`

## Why `WidgetProvider` Was Updated

To avoid CLSID drift and support process lifetime control, `WidgetProvider` now includes:

- `[Guid("...")]` attribute used as runtime CLSID source via `typeof(WidgetProvider).GUID`
- `GetEmptyWidgetListEvent()` so non-console mode can wait until all widgets are removed
- event reset/set logic in create/delete/recovery paths

## Program Entry Snippet (Copy/Paste)

```csharp
using System.Runtime.InteropServices;
using Xakpc.Widgets.Playground.Com;

namespace Xakpc.Widgets.Playground;

internal static class Program
{
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
        if (args.Length == 0 || !string.Equals(args[0], "-RegisterProcessAsComServer", StringComparison.Ordinal))
        {
            Console.WriteLine("Not launched for widget provider activation. Exiting.");
            return;
        }

        WinRT.ComWrappersSupport.InitializeComWrappers();

        uint cookie = 0;
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
            if (GetConsoleWindow() != IntPtr.Zero)
            {
                Console.WriteLine("Widget provider registered. Press ENTER to exit.");
                Console.ReadLine();
            }
            else
            {
                WidgetProvider.GetEmptyWidgetListEvent().WaitOne();
            }
        }
        finally
        {
            if (cookie != 0)
            {
                CoRevokeClassObject(cookie);
            }
        }
    }
}
```

## Flow Summary

1. process starts with `-RegisterProcessAsComServer`
2. app initializes WinRT COM wrappers
3. app registers `WidgetProviderFactory<WidgetProvider>` for `typeof(WidgetProvider).GUID`
4. host requests provider instances through COM class factory
5. app waits:
   - console mode: waits for manual Enter
   - non-console mode: waits until provider reports zero widgets
6. app revokes class registration before exit

## Image Placeholders

- `[IMAGE: create-guid-tool.png]` - Visual Studio Create GUID tool usage.

## Verify Step 08

Run:

```powershell
dotnet build Xakpc.Widgets.Playground.sln
```

Expected result: successful build with COM startup wiring.

## Common Mistakes

- using a manifest CLSID that does not match the provider `[Guid("...")]` value
- forgetting to call `WinRT.ComWrappersSupport.InitializeComWrappers()`
- registering COM class object but never revoking it on shutdown

## Next Step

Step 09 adds packaging and manifest declarations that point to this provider CLSID.
