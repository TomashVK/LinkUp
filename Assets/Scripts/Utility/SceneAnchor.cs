using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SceneAnchor : MonoBehaviour
{
    public enum AnchorMode { Scene, Object }

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

    [SerializeField] private AnchorMode mode;
    [SerializeField] private AnchorType anchor;
    [SerializeField] private float offsetX;
    [SerializeField] private float offsetY;

    [Header("Object mode")]
    [SerializeField] private Transform target;
    [Tooltip("Place the transform pivot at the target anchor point, ignoring this object's own size")]
    [SerializeField] private bool anchorPivot;

    void Start()
    {
        if (mode == AnchorMode.Object)
            StartCoroutine(ApplyObjectAnchorNextFrame());
        else
            ApplySceneAnchor();
    }

    private System.Collections.IEnumerator ApplyObjectAnchorNextFrame()
    {
        yield return null;
        ApplyObjectAnchor();
    }

    private void ApplySceneAnchor()
    {
        Rect sv = SafeAreaHelper.GetViewportRect();
        Vector2 norm = GetNorm();
        float vx = Mathf.Lerp(sv.xMin, sv.xMax, norm.x);
        float vy = Mathf.Lerp(sv.yMin, sv.yMax, norm.y);

        Vector3 anchorWorldPos = Camera.main.ViewportToWorldPoint(new Vector3(vx, vy, 10));
        Vector2 size = GetObjectSize();
        Vector2 pivot = new(0.5f - norm.x, 0.5f - norm.y);

        // Collider center may not sit at the transform pivot — subtract that offset so
        // the chosen edge of the collider lands on the screen anchor, not the pivot.
        Vector2 centerOffset = TryGetComponent<Collider2D>(out var col)
            ? (Vector2)col.bounds.center - (Vector2)transform.position
            : Vector2.zero;

        transform.position = anchorWorldPos + new Vector3(
            pivot.x * size.x - centerOffset.x + offsetX,
            pivot.y * size.y - centerOffset.y + offsetY,
            0);
    }

    private void ApplyObjectAnchor()
    {
        if (target == null)
        {
            Debug.LogError($"[SceneAnchor] '{name}' is in Object mode but Target is not assigned.", this);
            return;
        }

        Bounds targetBounds = GetBounds(target);
        Vector2 norm = GetNorm();

        float x, y;
        if (anchorPivot)
        {
            x = Mathf.Lerp(targetBounds.min.x, targetBounds.max.x, norm.x) + offsetX;
            y = Mathf.Lerp(targetBounds.min.y, targetBounds.max.y, norm.y) + offsetY;
        }
        else
        {
            Vector2 selfSize = GetObjectSize();
            Vector2 centerOffset = TryGetComponent<Collider2D>(out var col)
                ? (Vector2)col.bounds.center - (Vector2)transform.position
                : Vector2.zero;
            x = Mathf.Lerp(targetBounds.min.x, targetBounds.max.x, norm.x)
                + (norm.x - 0.5f) * selfSize.x - centerOffset.x + offsetX;
            y = Mathf.Lerp(targetBounds.min.y, targetBounds.max.y, norm.y)
                + (norm.y - 0.5f) * selfSize.y - centerOffset.y + offsetY;
        }

        transform.position = new Vector3(x, y, transform.position.z);
    }

    private Vector2 GetNorm() => anchor switch
    {
        AnchorType.TopLeft      => new Vector2(0f,   1f),
        AnchorType.TopCenter    => new Vector2(0.5f, 1f),
        AnchorType.TopRight     => new Vector2(1f,   1f),
        AnchorType.MiddleLeft   => new Vector2(0f,   0.5f),
        AnchorType.Center       => new Vector2(0.5f, 0.5f),
        AnchorType.MiddleRight  => new Vector2(1f,   0.5f),
        AnchorType.BottomLeft   => new Vector2(0f,   0f),
        AnchorType.BottomCenter => new Vector2(0.5f, 0f),
        AnchorType.BottomRight  => new Vector2(1f,   0f),
        _                       => new Vector2(0.5f, 0.5f),
    };

    private static Bounds GetBounds(Transform t)
    {
        if (t.TryGetComponent<Collider2D>(out var col)) return col.bounds;
        Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }
        return new Bounds(t.position, Vector3.zero);
    }

    private Vector2 GetObjectSize()
    {
        if (TryGetComponent<Renderer>(out var rend))  return rend.bounds.size;
        if (TryGetComponent<Collider2D>(out var col)) return col.bounds.size;
        return Vector2.zero;
    }
}
