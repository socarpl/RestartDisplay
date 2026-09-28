using System.Runtime.InteropServices;

// Direct COM calls avoid the built-in COM marshaler, which Native AOT does not support.
internal static unsafe class Shortcut
{
    [DllImport("ole32.dll")] private static extern int CoInitializeEx(nint reserved, uint flags);
    [DllImport("ole32.dll")] private static extern void CoUninitialize();
    [DllImport("ole32.dll")] private static extern int CoCreateInstance(in Guid clsid, nint outer, uint context, in Guid iid, out nint instance);

    public static void Create(string exe, int id, string destination)
    {
        Marshal.ThrowExceptionForHR(CoInitializeEx(0, 2));
        nint shell = 0, persist = 0;
        try
        {
            Guid clsid = new("00021401-0000-0000-C000-000000000046");
            Guid iid = new("000214F9-0000-0000-C000-000000000046");
            Marshal.ThrowExceptionForHR(CoCreateInstance(in clsid, 0, 1, in iid, out shell));
            // Shell links store the target separately from arguments; Shell quotes it on launch.
            SetString(shell, 20, "\"" + exe + "\""); // Quoted target, separate from arguments.
            SetString(shell, 11, id.ToString(System.Globalization.CultureInfo.InvariantCulture));
            SetString(shell, 9, System.IO.Path.GetDirectoryName(exe)!);
            SetString(shell, 7, $"Restart monitor {id}");
            Guid persistIid = new("0000010B-0000-0000-C000-000000000046");
            nint result;
            int hr = ((delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)(*(nint**)shell)[0])(shell, &persistIid, &result);
            Marshal.ThrowExceptionForHR(hr);
            persist = result;
            fixed (char* file = destination)
                Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, char*, int, int>)(*(nint**)persist)[6])(persist, file, 1));
        }
        finally
        {
            Release(persist);
            Release(shell);
            CoUninitialize();
        }
    }

    private static void SetString(nint instance, int slot, string value)
    {
        fixed (char* text = value)
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, char*, int>)(*(nint**)instance)[slot])(instance, text));
    }
    private static void Release(nint instance)
    {
        if (instance != 0) ((delegate* unmanaged[Stdcall]<nint, uint>)(*(nint**)instance)[2])(instance);
    }
}
