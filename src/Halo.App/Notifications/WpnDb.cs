using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace Halo.Notifications;

internal static class WpnDb
{
    private static readonly string DbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Microsoft", "Windows", "Notifications", "wpndatabase.db");

    public static (string launch, string activationType) LaunchFor(uint id)
    {
        string tmp = Path.Combine(Path.GetTempPath(), "halo-wpn-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            using (var s = new FileStream(DbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var d = new FileStream(tmp, FileMode.Create, FileAccess.Write))
                s.CopyTo(d);

            var xml = QueryPayload(tmp, id);
            if (string.IsNullOrEmpty(xml)) return ("", "");
            var root = XDocument.Parse(xml).Root;
            if (root is null || root.Name.LocalName != "toast") return ("", "");
            return ((string?)root.Attribute("launch") ?? "",
                    (string?)root.Attribute("activationType") ?? "");
        }
        catch { return ("", ""); }
        finally { try { if (File.Exists(tmp)) File.Delete(tmp); } catch { } }
    }

    private static string QueryPayload(string path, uint id)
    {
        if (sqlite3_open_v2(U8(path), out var db, SQLITE_OPEN_READONLY, IntPtr.Zero) != 0) return "";
        try
        {
            if (sqlite3_prepare_v2(db, U8("SELECT Payload FROM Notification WHERE Id=" + id), -1, out var st, IntPtr.Zero) != 0)
                return "";
            try
            {
                if (sqlite3_step(st) != SQLITE_ROW) return "";
                var p = sqlite3_column_blob(st, 0);
                int n = sqlite3_column_bytes(st, 0);
                if (p == IntPtr.Zero || n <= 0) return "";
                var b = new byte[n];
                Marshal.Copy(p, b, 0, n);
                return Encoding.UTF8.GetString(b);
            }
            finally { sqlite3_finalize(st); }
        }
        finally { sqlite3_close(db); }
    }

    private static byte[] U8(string s) => Encoding.UTF8.GetBytes(s + "\0");

    private const int SQLITE_OPEN_READONLY = 1, SQLITE_ROW = 100;
    private const string Dll = "winsqlite3.dll";
    [DllImport(Dll)] private static extern int sqlite3_open_v2(byte[] f, out IntPtr db, int flags, IntPtr vfs);
    [DllImport(Dll)] private static extern int sqlite3_prepare_v2(IntPtr db, byte[] sql, int n, out IntPtr stmt, IntPtr tail);
    [DllImport(Dll)] private static extern int sqlite3_step(IntPtr stmt);
    [DllImport(Dll)] private static extern IntPtr sqlite3_column_blob(IntPtr stmt, int c);
    [DllImport(Dll)] private static extern int sqlite3_column_bytes(IntPtr stmt, int c);
    [DllImport(Dll)] private static extern int sqlite3_finalize(IntPtr stmt);
    [DllImport(Dll)] private static extern int sqlite3_close(IntPtr db);
}
