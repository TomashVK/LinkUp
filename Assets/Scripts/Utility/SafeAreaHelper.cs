using UnityEngine;

public static class SafeAreaHelper
{
    // Returns viewport-space safe area: xMin=left, xMax=right, yMin=bottom, yMax=top
    public static Rect GetViewportRect()
    {
        Rect sa = Screen.safeArea;
        return new Rect(
            sa.x / Screen.width,
            sa.y / Screen.height,
            sa.width  / Screen.width,
            sa.height / Screen.height);
    }
}
