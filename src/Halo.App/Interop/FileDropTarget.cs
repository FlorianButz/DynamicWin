using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Halo.Widgets;

namespace Halo.Interop;

internal sealed class FileDropTarget : Win32.IDropTarget
{
    private const int S_OK = 0;

    public int DragEnter(IDataObject data, int keyState, Win32.POINTL pt, ref int effect)
    {
        if (FileDrag.Dragging) { effect = Win32.DROPEFFECT_NONE; return S_OK; }
        bool files = HasHdrop(data);
        FileTray.SetDragActive(files);
        effect = files ? Win32.DROPEFFECT_COPY : Win32.DROPEFFECT_NONE;
        return S_OK;
    }

    public int DragOver(int keyState, Win32.POINTL pt, ref int effect)
    {
        effect = FileTray.DragActive ? Win32.DROPEFFECT_COPY : Win32.DROPEFFECT_NONE;
        return S_OK;
    }

    public int DragLeave()
    {
        FileTray.SetDragActive(false);
        return S_OK;
    }

    public int Drop(IDataObject data, int keyState, Win32.POINTL pt, ref int effect)
    {
        foreach (var p in GetPaths(data)) FileTray.Add(p);
        FileTray.SetDragActive(false);
        effect = Win32.DROPEFFECT_COPY;
        return S_OK;
    }

    private static FORMATETC HdropFormat() => new()
    {
        cfFormat = Win32.CF_HDROP,
        dwAspect = DVASPECT.DVASPECT_CONTENT,
        lindex = -1,
        tymed = TYMED.TYMED_HGLOBAL,
    };

    private static bool HasHdrop(IDataObject data)
    {
        try { var f = HdropFormat(); return data.QueryGetData(ref f) == S_OK; }
        catch { return false; }
    }

    private static string[] GetPaths(IDataObject data)
    {
        var f = HdropFormat();
        STGMEDIUM m = default;
        try
        {
            data.GetData(ref f, out m);
            if (m.unionmember == IntPtr.Zero) return Array.Empty<string>();
            uint n = Win32.DragQueryFile(m.unionmember, 0xFFFFFFFF, null, 0);
            var list = new List<string>((int)n);
            var sb = new StringBuilder(1024);
            for (uint i = 0; i < n; i++)
            {
                sb.Clear();
                if (Win32.DragQueryFile(m.unionmember, i, sb, (uint)sb.Capacity) > 0)
                    list.Add(sb.ToString());
            }
            return list.ToArray();
        }
        catch { return Array.Empty<string>(); }
        finally { Win32.ReleaseStgMedium(ref m); }
    }
}
