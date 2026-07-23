using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace Halo.Interop;

internal static class FileDrag
{

    public static volatile bool Dragging;

    private static readonly Guid BHID_DataObject = new("B8C0BD9F-ED24-455C-83E6-D5390C4FE8C4");
    private static readonly Guid IID_IDataObject = new("0000010e-0000-0000-C000-000000000046");

    private const int DRAGDROP_S_DROP = 0x00040100;

    public static bool Out(string path) => Out(new[] { path });

    public static bool Out(string[] paths)
    {
        if (paths == null || paths.Length == 0) return false;
        var pidls = new List<IntPtr>(paths.Length);
        try
        {
            foreach (var p in paths)
                if (Win32.SHParseDisplayName(p, IntPtr.Zero, out var pidl, 0, out _) == 0 && pidl != IntPtr.Zero)
                    pidls.Add(pidl);
            if (pidls.Count == 0) return false;

            if (Win32.SHCreateShellItemArrayFromIDLists((uint)pidls.Count, pidls.ToArray(), out var arr) != 0 || arr == null)
                return false;
            try
            {
                if (arr.BindToHandler(IntPtr.Zero, BHID_DataObject, IID_IDataObject, out var pdo) != 0
                    || pdo is not ComTypes.IDataObject data)
                    return false;
                Dragging = true;
                int hr;
                try { hr = Win32.SHDoDragDrop(IntPtr.Zero, data, new DropSource(), Win32.DROPEFFECT_COPY | Win32.DROPEFFECT_MOVE, out _); }
                finally { Dragging = false; }

                return hr == DRAGDROP_S_DROP;
            }
            finally { Marshal.ReleaseComObject(arr); }
        }
        catch { Dragging = false; return false; }
        finally { foreach (var pidl in pidls) Win32.ILFree(pidl); }
    }

    private sealed class DropSource : Win32.IDropSource
    {
        private const int S_OK = 0, DRAGDROP_S_DROP = 0x00040100, DRAGDROP_S_CANCEL = 0x00040101,
            DRAGDROP_S_USEDEFAULTCURSORS = 0x00040102, MK_LBUTTON = 0x0001;

        public int QueryContinueDrag(int escapePressed, int keyState)
        {
            if (escapePressed != 0) return DRAGDROP_S_CANCEL;
            if ((keyState & MK_LBUTTON) == 0) return DRAGDROP_S_DROP;
            return S_OK;
        }

        public int GiveFeedback(int effect) => DRAGDROP_S_USEDEFAULTCURSORS;
    }
}
