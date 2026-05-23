using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SceneAnchor : MonoBehaviour
{
    public enum AnchorType
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        Center,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    [SerializeField] private AnchorType anchor;
    [SerializeField] private float offsetX;
    [SerializeField] private float offsetY;

    void Start()
    {
        Rect sv = SafeAreaHelper.GetViewportRect();
        float safeLeft   = sv.xMin;
        float safeRight  = sv.xMax;
        float safeBottom = sv.yMin;
        float safeTop    = sv.yMax;

        // Normalized anchor position: 0 = left/bottom edge, 0.5 = center, 1 = right/top edge.
        // Kept separate from viewport so the pivot formula stays correct regardless of safe area offset.
        Vector2 norm = anchor switch
        {
            AnchorType.TopLeft      => new Vector2(0f,   1f  ),
            AnchorType.TopCenter    => new Vector2(0.5f, 1f  ),
            AnchorType.TopRight     => new Vector2(1f,   1f  ),
            AnchorType.MiddleLeft   => new Vector2(0f,   0.5f),
            AnchorType.Center       => new Vector2(0.5f, 0.5f),
            AnchorType.MiddleRight  => new Vector2(1f,   0.5f),
            AnchorType.BottomLeft   => new Vector2(0f,   0f  ),
            AnchorType.BottomCenter => new Vector2(0.5f, 0f  ),
            AnchorType.BottomRight  => new Vector2(1f,   0f  ),
            _                       => new Vector2(0.5f, 0.5f),
        };

        float vx = Mathf.Lerp(safeLeft, safeRight, norm.x);
        float vy = Mathf.Lerp(safeBottom, safeTop,  norm.y);

        Vector3 cornerPos = Camera.main.ViewportToWorldPoint(new Vector3(vx, vy, 10));
        Vector2 size  = GetObjectSize();
        Vector2 pivot = new(0.5f - norm.x, 0.5f - norm.y);

        transform.position = cornerPos + new Vector3(
            pivot.x * size.x + offsetX,
            pivot.y * size.y + offsetY,
            0);
    }

    private Vector2 GetObjectSize()
    {
        if (TryGetComponent<Renderer>(out var rend)) return rend.bounds.size;
        if (TryGetComponent<Collider2D>(out var col)) return col.bounds.size;
        return Vector2.zero;
    }
}
