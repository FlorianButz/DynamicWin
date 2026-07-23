using System;
using System.Collections.Generic;
using System.Drawing;

namespace Halo.Widgets;

internal readonly record struct AgentNotice(string? State, DateTimeOffset? CompactedAt, string? Message)
{
    internal static AgentNotice None => new(null, null, null);
}

internal interface IWidget
{
    string Icon { get; }

    Bitmap? IconImage => null;

    bool IsActive { get; }
    int Version { get; }

    bool Animating => false;

    Color? Ring => null;

    float RingProgress => -1f;

    AgentNotice AgentNotice => AgentNotice.None;

    IEnumerable<int> OwnerPids => Array.Empty<int>();

    void DrawContent(Graphics g, int w, int h, float expandFade);

    void DrawCollapsed(Graphics g, int w, int h, float fade) { }

    IReadOnlyList<(RectangleF rect, Action<PointF> onClick)> Buttons(int w, int h);
}
