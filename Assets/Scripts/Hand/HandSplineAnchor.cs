using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(SplineContainer))]
public class HandSplineAnchor : MonoBehaviour
{
    [SerializeField][Range(0f, 0.45f)] private float horizontalPadding = 0.05f;
    [SerializeField][Range(0f, 1f)]    private float verticalPosition  = 0.1f;

    private SplineContainer splineContainer;
    private float3[] originalPositions;

    private void Awake()
    {
        splineContainer = GetComponent<SplineContainer>();

        Spline spline = splineContainer.Spline;
        originalPositions = new float3[spline.Count];
        for (int i = 0; i < spline.Count; i++)
            originalPositions[i] = spline[i].Position;
    }

    private void Start()
    {
        FitToScreen();
    }

    private void FitToScreen()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float depth = -cam.transform.position.z;

        Rect sv = SafeAreaHelper.GetViewportRect();
        float safeLeft   = sv.xMin;
        float safeRight  = sv.xMax;
        float safeBottom = sv.yMin;
        float safeTop    = sv.yMax;

        Vector3 worldSafeLeft  = cam.ViewportToWorldPoint(new Vector3(safeLeft,  0f, depth));
        Vector3 worldSafeRight = cam.ViewportToWorldPoint(new Vector3(safeRight, 0f, depth));
        float safeWorldWidth = worldSafeRight.x - worldSafeLeft.x;

        float leftX  = worldSafeLeft.x  + safeWorldWidth * horizontalPadding;
        float rightX = worldSafeRight.x - safeWorldWidth * horizontalPadding;

        float safeWorldBottom = cam.ViewportToWorldPoint(new Vector3(0f, safeBottom, depth)).y;
        float safeWorldTop    = cam.ViewportToWorldPoint(new Vector3(0f, safeTop,    depth)).y;
        float targetBaseY     = Mathf.Lerp(safeWorldBottom, safeWorldTop, verticalPosition);

        float origMinX = float.MaxValue;
        float origMaxX = float.MinValue;
        float origMinY = float.MaxValue;

        foreach (float3 p in originalPositions)
        {
            if (p.x < origMinX) origMinX = p.x;
            if (p.x > origMaxX) origMaxX = p.x;
            if (p.y < origMinY) origMinY = p.y;
        }

        float origWidth = origMaxX - origMinX;
        float yShift    = targetBaseY - origMinY;

        Spline spline = splineContainer.Spline;
        for (int i = 0; i < originalPositions.Length; i++)
        {
            float3 orig = originalPositions[i];
            float tX   = origWidth > 0f ? (orig.x - origMinX) / origWidth : 0.5f;
            float newX = Mathf.Lerp(leftX, rightX, tX);
            float newY = orig.y + yShift;

            BezierKnot knot = spline[i];
            knot.Position = new float3(newX, newY, orig.z);
            spline.SetKnot(i, knot);
        }
    }
}
