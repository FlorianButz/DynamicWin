using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;

namespace Halo.Widgets;

internal sealed class FileTray : IWidget
{
    private static readonly Color White = Color.FromArgb(238, 255, 255, 255);
    private static readonly Color Dim = Color.FromArgb(150, 255, 255, 255);
    private static readonly Color Track = Color.FromArgb(46, 255, 255, 255);
    private static readonly Color Accent = Color.FromArgb(120, 185, 255);
    private static readonly Color Red = Color.FromArgb(255, 120, 110);
    private static readonly FontFamily Fluent = new("Segoe Fluent Icons");

    private static readonly object _lock = new();
    private static readonly List<string> _paths = new();
    private static readonly HashSet<string> _selected = new(StringComparer.OrdinalIgnoreCase);
    private static int _version;
    public static volatile bool DragActive;

    private static readonly Dictionary<string, PointF> _anim = new(StringComparer.OrdinalIgnoreCase);
    private static volatile bool _settled = true;

    public static int ReorderFrom = -1, ReorderTo = -1;

    private const int MaxItems = 30;
    private static readonly string StorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Halo", "tray.txt");

    static FileTray() => Load();

    public string Icon => ((char)0xE7B8).ToString();

    public bool IsActive { get { lock (_lock) return DragActive || _paths.Count > 0; } }
    public int Version => _version + (DragActive ? (int)(Environment.TickCount64 / 60) : 0);
    public bool Animating => DragActive || !_settled;

    public static void SetDragActive(bool on)
    {
        if (DragActive == on) return;
        DragActive = on;
        Interlocked.Increment(ref _version);
    }

    public static void Add(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        string full;
        try { full = Path.GetFullPath(path); } catch { return; }

        if (full.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                dynamic sh = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
                string t = sh.CreateShortcut(full).TargetPath;
                if (!string.IsNullOrWhiteSpace(t) && (File.Exists(t) || Directory.Exists(t))) full = Path.GetFullPath(t);
            }
            catch { }
        }
        if (!File.Exists(full) && !Directory.Exists(full)) return;
        lock (_lock)
        {
            _paths.RemoveAll(p => string.Equals(p, full, StringComparison.OrdinalIgnoreCase));
            _paths.Insert(0, full);
            if (_paths.Count > MaxItems) _paths.RemoveRange(MaxItems, _paths.Count - MaxItems);
            Save();
        }
        Interlocked.Increment(ref _version);
    }

    private static void Remove(string path)
    {
        lock (_lock) { _paths.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase)); _selected.Remove(path); Save(); }
        Interlocked.Increment(ref _version);
    }

    public static void RemovePaths(string[] paths)
    {
        if (paths == null || paths.Length == 0) return;
        lock (_lock)
        {
            foreach (var p in paths) { _paths.RemoveAll(x => string.Equals(x, p, StringComparison.OrdinalIgnoreCase)); _selected.Remove(p); }
            Save();
        }
        Interlocked.Increment(ref _version);
    }

    public static int SelectedCount { get { lock (_lock) return _selected.Count; } }
    private static bool IsSelected(string path) { lock (_lock) return _selected.Contains(path); }

    public static void ToggleSelect(string path)
    {
        lock (_lock) { if (!_selected.Remove(path)) _selected.Add(path); }
        Interlocked.Increment(ref _version);
    }

    public static void ClearSelection()
    {
        lock (_lock) { if (_selected.Count == 0) return; _selected.Clear(); }
        Interlocked.Increment(ref _version);
    }

    public static void RemoveSelected()
    {
        lock (_lock) { if (_selected.Count == 0) return; _paths.RemoveAll(_selected.Contains); _selected.Clear(); Save(); }
        Interlocked.Increment(ref _version);
    }

    public static string[] SelectionOrRow(string grabbed)
    {
        lock (_lock)
            return _selected.Count > 0 && _selected.Contains(grabbed)
                ? _paths.Where(_selected.Contains).ToArray()
                : new[] { grabbed };
    }

    private static int IndexOf(string path) { lock (_lock) return _paths.FindIndex(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase)); }

    public static void BeginReorder(string grabbed) { ReorderFrom = ReorderTo = IndexOf(grabbed); Interlocked.Increment(ref _version); }
    public static void UpdateReorder(int to) { if (to != ReorderTo) { ReorderTo = to; Interlocked.Increment(ref _version); } }
    public static void CancelReorder() { ReorderFrom = ReorderTo = -1; Interlocked.Increment(ref _version); }

    public static void CommitReorder()
    {
        int from = ReorderFrom, to = ReorderTo;
        ReorderFrom = ReorderTo = -1;
        lock (_lock)
        {
            if (from >= 0 && to >= 0 && from != to && from < _paths.Count)
            {
                var moved = _paths[from];
                _paths.RemoveAt(from);
                _paths.Insert(Math.Clamp(to, 0, _paths.Count), moved);
                Save();
            }
        }
        Interlocked.Increment(ref _version);
    }

    private static long _pruneAt;

    private static string[] Snapshot()
    {
        lock (_lock)
        {
            if (Environment.TickCount64 - _pruneAt > 2000)
            {
                _pruneAt = Environment.TickCount64;
                if (_paths.RemoveAll(p => !File.Exists(p) && !Directory.Exists(p)) > 0)
                {
                    _selected.RemoveWhere(s => !_paths.Contains(s, StringComparer.OrdinalIgnoreCase));
                    Save();
                    Interlocked.Increment(ref _version);
                }
            }
            return _paths.ToArray();
        }
    }

    private static void Load()
    {
        try
        {
            if (!File.Exists(StorePath)) return;
            lock (_lock)
                foreach (var line in File.ReadAllLines(StorePath))
                {
                    var p = line.Trim();
                    if (p.Length > 0 && (File.Exists(p) || Directory.Exists(p))
                        && !_paths.Contains(p, StringComparer.OrdinalIgnoreCase))
                        _paths.Add(p);
                }
        }
        catch { }
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StorePath)!);
            File.WriteAllLines(StorePath, _paths);
        }
        catch { }
    }

    public void DrawCollapsed(Graphics g, int w, int h, float fade)
    {
        var items = Snapshot();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float sz = h - 14f, ix = 9, iy = (h - sz) / 2f;

        if (DragActive)
        {
            float pulse = 0.5f - 0.5f * MathF.Cos(Environment.TickCount % 2400 / 2400f * MathF.Tau);
            using (var pb = new SolidBrush(Mul(Accent, fade * (0.06f + 0.14f * pulse))))
            using (var pp = Fx.PillPath(w, h, h / 2f))
                g.FillPath(pb, pp);
            DrawTile(g, ix, iy, sz, fade, null);
            DrawLabel(g, "Drop to add", ix + sz + 12, w, h, fade);
            return;
        }

        if (items.Length <= 1)
        {
            DrawTile(g, ix, iy, sz, fade, items.Length == 1 ? Halo.Notifications.ShellIcon.ForPath(items[0]) : null);
            DrawLabel(g, items.Length == 1 ? Path.GetFileName(items[0]) : "Empty", ix + sz + 12, w, h, fade);
            return;
        }

        int n = Math.Min(4, items.Length);
        float step = sz * 0.58f;
        for (int i = n - 1; i >= 0; i--)
        {

            using (var kb = new SolidBrush(Mul(Color.FromArgb(255, 12, 12, 14), fade)))
            using (var kp = Fx.Rounded(new RectangleF(ix + i * step - 1.5f, iy - 1.5f, sz + 3, sz + 3), sz * 0.28f))
                g.FillPath(kb, kp);
            DrawTile(g, ix + i * step, iy, sz, fade, Halo.Notifications.ShellIcon.ForPath(items[i]));
        }
        DrawLabel(g, $"{items.Length} files", ix + (n - 1) * step + sz + 12, w, h, fade);
    }

    private static void DrawLabel(Graphics g, string s, float x, int w, int h, float fade)
    {
        using var f = new Font("Segoe UI Semibold", 14f, GraphicsUnit.Pixel);
        using var b = new SolidBrush(Mul(White, fade));
        DrawEllipsized(g, s, f, b, x, (h - 18f) / 2f, w - x - 14, 18);
    }

    private const float Pad = 22, HeaderH = 56, ColGap = 10, RowGap = 6, CellH = 44;
    private const int Cols = 3;

    private static float CellW(int w) => (w - 2 * Pad - (Cols - 1) * ColGap) / Cols;
    private static int RowsFor(int h) => Math.Max(1, (int)((h - HeaderH - 10) / (CellH + RowGap)));
    private static int VisibleCells(int w, int h) => Cols * RowsFor(h);
    private static RectangleF CellRect(int i, int w, int h)
    {
        int col = i % Cols, row = i / Cols;
        return new RectangleF(Pad + col * (CellW(w) + ColGap), HeaderH + row * (CellH + RowGap), CellW(w), CellH);
    }
    private static RectangleF CellXRect(RectangleF cell) => new(cell.Right - 22, cell.Y + 3, 18, 18);

    public void DrawContent(Graphics g, int w, int h, float fade)
    {
        if (fade <= 0.01f) { _settled = true; _anim.Clear(); return; }
        var items = Snapshot();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float pulse = 0.5f + 0.5f * MathF.Sin(Environment.TickCount64 / 480f);
        Fx.Glow(g, w, h, fade * (DragActive ? 0.6f + 0.4f * pulse : 0.9f), w * 0.5f, h * 0.4f, w * 0.9f, h * 1.2f, 34, Accent);

        using var title = new Font("Segoe UI Semibold", 21f, GraphicsUnit.Pixel);
        using var body = new Font("Segoe UI", 14f, GraphicsUnit.Pixel);
        using (var tb = new SolidBrush(Mul(White, fade)))
            g.DrawString("File Tray", title, tb, Pad + 20, 14);

        int sel = SelectedCount;
        if (sel > 0) DrawRemoveChip(g, w, fade, sel);
        else if (items.Length > 0)
            using (var cb = new SolidBrush(Mul(Dim, fade)))
            using (var rf = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Far })
                g.DrawString($"{items.Length} item{(items.Length == 1 ? "" : "s")}", body, cb,
                    new RectangleF(Pad, 20, w - Pad * 2, 24), rf);

        if (DragActive || items.Length == 0) { DrawDropZone(g, w, h, fade); return; }

        var order = DisplayOrder(items);
        string? grabbed = ReorderFrom >= 0 && ReorderFrom < items.Length ? items[ReorderFrom] : null;
        int vis = VisibleCells(w, h);
        int shown = Math.Min(vis, order.Length);

        bool settled = true;
        RectangleF grabbedRect = default; bool haveGrab = false;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < shown; i++)
        {
            string path = order[i];
            seen.Add(path);
            var target = CellRect(i, w, h);
            if (!_anim.TryGetValue(path, out var cur)) cur = target.Location;
            float nx = cur.X + (target.X - cur.X) * 0.24f, ny = cur.Y + (target.Y - cur.Y) * 0.24f;
            if (MathF.Abs(nx - target.X) < 0.4f && MathF.Abs(ny - target.Y) < 0.4f) { nx = target.X; ny = target.Y; }
            else settled = false;
            _anim[path] = new PointF(nx, ny);
            var rect = new RectangleF(nx, ny, target.Width, target.Height);
            if (path == grabbed) { grabbedRect = rect; haveGrab = true; continue; }
            DrawCell(g, rect, path, fade, IsSelected(path), false);
        }
        if (haveGrab) DrawCell(g, grabbedRect, grabbed!, fade, IsSelected(grabbed!), true);

        if (_anim.Count > shown)
            foreach (var k in _anim.Keys.Where(k => !seen.Contains(k)).ToList()) _anim.Remove(k);
        _settled = settled;
        if (!settled) Interlocked.Increment(ref _version);

        if (order.Length > vis)
            using (var mb = new SolidBrush(Mul(Dim, fade)))
            using (var cf = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Far })
                g.DrawString($"+{order.Length - vis} more", body, mb, new RectangleF(Pad, h - Pad + 2, w - Pad * 2, 18), cf);
    }

    private static string[] DisplayOrder(string[] items)
    {
        int from = ReorderFrom, to = ReorderTo;
        if (from < 0 || to < 0 || from == to || from >= items.Length) return items;
        var list = new List<string>(items);
        var moved = list[from];
        list.RemoveAt(from);
        list.Insert(Math.Clamp(to, 0, list.Count), moved);
        return list.ToArray();
    }

    public int RowIndexAt(int w, int h, PointF p)
    {
        int count = Math.Min(Snapshot().Length, VisibleCells(w, h));
        if (count == 0) return 0;
        int col = Math.Clamp((int)((p.X - Pad) / (CellW(w) + ColGap)), 0, Cols - 1);
        int row = Math.Clamp((int)((p.Y - HeaderH) / (CellH + RowGap)), 0, (count - 1) / Cols);
        return Math.Clamp(row * Cols + col, 0, count - 1);
    }

    private static RectangleF RemoveChipRect(int w) => new(w - Pad - 132, 16, 132, 28);

    private void DrawRemoveChip(Graphics g, int w, float fade, int n)
    {
        var r = RemoveChipRect(w);
        bool hov = WidgetInput.Over && r.Contains(WidgetInput.Mouse);
        using (var bg = new SolidBrush(Mul(Color.FromArgb(hov ? 60 : 34, Red), fade)))
        using (var p = Fx.Rounded(r, r.Height / 2f))
            g.FillPath(bg, p);
        DrawGlyph(g, new RectangleF(r.X + 12, r.Y, 26, r.Height), ((char)0xE74D).ToString(), 15f, fade * (hov ? 1f : 0.85f), Red);
        using var f = new Font("Segoe UI Semibold", 14f, GraphicsUnit.Pixel);
        using var b = new SolidBrush(Mul(hov ? White : Color.FromArgb(255, 200, 195), fade));
        using var sf = new StringFormat { LineAlignment = StringAlignment.Center };
        g.DrawString($"Remove {n}", f, b, new RectangleF(r.X + 34, r.Y, r.Width - 40, r.Height), sf);
    }

    private void DrawDropZone(Graphics g, int w, int h, float fade)
    {
        var box = new RectangleF(Pad, HeaderH - 8, w - Pad * 2, h - (HeaderH - 8) - Pad + 6);
        bool active = DragActive;
        float pulse = 0.5f + 0.5f * MathF.Sin(Environment.TickCount64 / 600f);
        float border = fade * (active ? 0.75f + 0.25f * pulse : 0.5f);

        using (var fillp = Fx.Rounded(box, 18))
        {
            using (var fb = new SolidBrush(Mul(Accent, fade * (active ? 0.10f + 0.06f * pulse : 0.045f))))
                g.FillPath(fb, fillp);
            using (var pen = new Pen(Mul(active ? Accent : Dim, border), active ? 2.4f : 1.6f)
                   { DashStyle = DashStyle.Dash, DashPattern = new[] { 5f, 5f } })
                g.DrawPath(pen, fillp);
        }

        float cx = box.X + box.Width / 2f, cy = box.Y + box.Height / 2f - 14, rad = 30;
        using (var cb = new SolidBrush(Mul(Accent, fade * (active ? 0.24f + 0.1f * pulse : 0.14f))))
            g.FillEllipse(cb, cx - rad, cy - rad, rad * 2, rad * 2);
        DrawGlyph(g, new RectangleF(cx - rad, cy - rad, rad * 2, rad * 2), ((char)0xE7B8).ToString(),
            30f, fade * (active ? 1f : 0.85f), active ? White : Accent);

        using var f1 = new Font("Segoe UI Semibold", 17f, GraphicsUnit.Pixel);
        using var f2 = new Font("Segoe UI", 13f, GraphicsUnit.Pixel);
        using var sf = new StringFormat { Alignment = StringAlignment.Center };
        using (var b1 = new SolidBrush(Mul(White, fade)))
            g.DrawString(active ? "Release to add" : "Drop files here", f1, b1, new RectangleF(box.X, cy + rad + 6, box.Width, 24), sf);
        if (!active)
            using (var b2 = new SolidBrush(Mul(Dim, fade)))
                g.DrawString("they'll stay in the tray", f2, b2, new RectangleF(box.X, cy + rad + 30, box.Width, 20), sf);
    }

    private void DrawCell(Graphics g, RectangleF cell, string path, float fade, bool selected, bool lifted)
    {
        bool hov = WidgetInput.Over && cell.Contains(WidgetInput.Mouse);
        Color bg = lifted ? Color.FromArgb(52, Accent)
            : selected ? Color.FromArgb(34, Accent)
            : hov ? Color.FromArgb(26, 255, 255, 255)
            : Color.FromArgb(15, 255, 255, 255);
        using (var b = new SolidBrush(Mul(bg, fade)))
        using (var cp = Fx.Rounded(cell, 10))
        {
            g.FillPath(b, cp);
            if (selected || lifted)
                using (var pen = new Pen(Mul(Color.FromArgb(lifted ? 150 : 90, Accent), fade), 1f))
                    g.DrawPath(pen, cp);
        }

        float ico = cell.Height - 16;
        DrawTile(g, cell.X + 8, cell.Y + 8, ico, fade, Halo.Notifications.ShellIcon.ForPath(path));

        float tx = cell.X + 8 + ico + 9, tw = cell.Right - tx - (hov ? 24 : 8);
        using var nf = new Font("Segoe UI Semibold", 13.5f, GraphicsUnit.Pixel);
        using var ff = new Font("Segoe UI", 10.5f, GraphicsUnit.Pixel);
        using (var nb = new SolidBrush(Mul(White, fade)))
            DrawEllipsized(g, Path.GetFileName(path), nf, nb, tx, cell.Y + 6, tw, 18);
        using (var db = new SolidBrush(Mul(Dim, fade)))
            DrawEllipsized(g, Dir(path), ff, db, tx, cell.Y + 24, tw, 14);

        if (hov)
        {
            var xr = CellXRect(cell);
            bool hovX = xr.Contains(WidgetInput.Mouse);
            using (var xb = new SolidBrush(Mul(Color.FromArgb(hovX ? 70 : 40, Red), fade)))
                g.FillEllipse(xb, xr);
            DrawGlyph(g, xr, ((char)0xE711).ToString(), xr.Width * 0.36f, fade * (hovX ? 1f : 0.85f),
                hovX ? Color.FromArgb(255, 130, 120) : White);
        }
    }

    private static string Dir(string path)
    {
        try { return Path.GetFileName(Path.GetDirectoryName(path) ?? path) is { Length: > 0 } d ? d : path; }
        catch { return path; }
    }

    public IReadOnlyList<(RectangleF rect, Action<PointF> onClick)> Buttons(int w, int h)
    {
        var items = Snapshot();
        if (DragActive || items.Length == 0) return Array.Empty<(RectangleF, Action<PointF>)>();
        var list = new List<(RectangleF, Action<PointF>)>();
        if (SelectedCount > 0) list.Add((RemoveChipRect(w), _ => RemoveSelected()));
        int count = Math.Min(items.Length, VisibleCells(w, h));
        for (int i = 0; i < count; i++)
        {
            string path = items[i];
            list.Add((CellXRect(CellRect(i, w, h)), _ => Remove(path)));
        }
        return list;
    }

    public string? RowPathAt(int w, int h, PointF p)
    {
        var items = Snapshot();
        if (DragActive || items.Length == 0) return null;
        int count = Math.Min(items.Length, VisibleCells(w, h));
        for (int i = 0; i < count; i++)
        {
            var cell = CellRect(i, w, h);
            if (cell.Contains(p) && !CellXRect(cell).Contains(p)) return items[i];
        }
        return null;
    }

    public static void Open(string path)
    {
        try { Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true }); } catch { }
    }

    private static void DrawTile(Graphics g, float x, float y, float sz, float fade, Bitmap? icon)
    {
        var box = new RectangleF(x, y, sz, sz);
        using var path = Fx.Rounded(box, sz * 0.26f);
        if (icon != null)
        {
            int s = Math.Max(1, (int)Math.Ceiling(sz));
            using var scaled = new Bitmap(s, s, PixelFormat.Format32bppPArgb);
            using (var sg = Graphics.FromImage(scaled))
            {
                sg.InterpolationMode = InterpolationMode.HighQualityBicubic;
                sg.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using var ia = new ImageAttributes();
                ia.SetWrapMode(WrapMode.TileFlipXY);
                ia.SetColorMatrix(new ColorMatrix { Matrix33 = fade });
                int side = Math.Min(icon.Width, icon.Height);
                sg.DrawImage(icon, new Rectangle(0, 0, s, s), (icon.Width - side) / 2, (icon.Height - side) / 2,
                    side, side, GraphicsUnit.Pixel, ia);
            }
            using var tb = new TextureBrush(scaled) { WrapMode = WrapMode.Clamp };
            tb.TranslateTransform(box.X, box.Y);
            g.FillPath(tb, path);
        }
        else
        {
            using var gb = new SolidBrush(Mul(Track, fade));
            g.FillPath(gb, path);
            DrawGlyph(g, box, ((char)0xE7B8).ToString(), sz * 0.5f, fade);
        }
    }

    private static void DrawGlyph(Graphics g, RectangleF r, string glyph, float px, float fade, Color? tint = null)
    {
        using var path = new GraphicsPath();
        using var sf = new StringFormat(StringFormat.GenericTypographic);
        path.AddString(glyph, Fluent, (int)FontStyle.Regular, px, PointF.Empty, sf);
        path.Flatten();
        var bnd = path.GetBounds();
        if (bnd.Width <= 0 || bnd.Height <= 0) return;
        using var m = new Matrix();
        m.Translate(MathF.Round(r.X + (r.Width - bnd.Width) / 2f - bnd.X),
                    MathF.Round(r.Y + (r.Height - bnd.Height) / 2f - bnd.Y));
        path.Transform(m);
        using var br = new SolidBrush(Mul(tint ?? White, fade * 0.9f));
        g.FillPath(br, path);
    }

    private static void DrawEllipsized(Graphics g, string s, Font f, Brush b, float x, float y, float w, float h)
    {
        using var sf = new StringFormat(StringFormat.GenericTypographic)
        { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        g.DrawString(s, f, b, new RectangleF(x, y, w, h), sf);
    }

    private static Color Mul(Color c, float a) => Color.FromArgb((int)(c.A * a), c.R, c.G, c.B);
}
