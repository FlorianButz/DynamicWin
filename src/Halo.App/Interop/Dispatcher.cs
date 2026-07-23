using System;
using System.Runtime.InteropServices;

namespace Halo.Interop;

internal static class Dispatcher
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Options
    {
        public int dwSize;
        public int threadType;
        public int apartmentType;
    }

    [DllImport("CoreMessaging.dll")]
    private static extern int CreateDispatcherQueueController(Options options,
        [MarshalAs(UnmanagedType.IUnknown)] out object controller);

    private static object? _controller;

    public static void Ensure()
    {
        if (_controller != null) return;
        var o = new Options { dwSize = Marshal.SizeOf<Options>(), threadType = 2, apartmentType = 2 };
        int hr = CreateDispatcherQueueController(o, out _controller);
        if (hr < 0)
            throw new InvalidOperationException($"CreateDispatcherQueueController failed 0x{hr:X8}");
    }
}
