namespace Halo.Shell;

public static class NotchGeometry
{
    public static (int x, int y, int w, int h) CollapsedRect(int workLeft, int workTop, int workWidth, int collapsedWidth, int collapsedHeight)
        => (workLeft + (workWidth - collapsedWidth) / 2, workTop, collapsedWidth, collapsedHeight);

    public static (int x, int y, int w, int h) ExpandedRect(int workLeft, int workTop, int workWidth, int expandedWidth, int expandedHeight)
        => (workLeft + (workWidth - expandedWidth) / 2, workTop, expandedWidth, expandedHeight);
}
