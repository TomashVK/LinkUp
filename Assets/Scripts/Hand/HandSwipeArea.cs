using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HandSwipeArea : MonoBehaviour
{
    public static event System.Action<float> Scrolled;

    private Collider2D area;
    private Camera mainCamera;
    private bool tracking;
    private float lastWorldX;
    private float zDist;

    private void Awake()
    {
        area = GetComponent<Collider2D>();
        mainCamera = Camera.main;
    }

    private void Start()
    {
        zDist = mainCamera.WorldToScreenPoint(transform.position).z;
    }

    private void OnEnable()
    {
        PointerInputService.Instance.Pressed += OnPressed;
        PointerInputService.Instance.Released += OnReleased;
    }

    private void OnDisable()
    {
        PointerInputService.Instance.Pressed -= OnPressed;
        PointerInputService.Instance.Released -= OnReleased;
    }

    private void OnPressed(Vector2 screenPos)
    {
        if (HandManager.IsAnimating) return;

        Vector3 worldPos = ScreenToWorld(screenPos);
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);

        bool hitArea = false;
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<Card>(out _)) return;
            if (hit == area) hitArea = true;
        }

        if (!hitArea) return;
        tracking = true;
        lastWorldX = worldPos.x;
    }

    private void OnReleased(Vector2 _) => tracking = false;

    private void LateUpdate()
    {
        if (!tracking) return;
        float worldX = ScreenToWorld(PointerInputService.Instance.Position).x;
        float delta = worldX - lastWorldX;
        lastWorldX = worldX;
        if (Mathf.Abs(delta) > 0.0001f)
            Scrolled?.Invoke(delta);
    }

    private Vector3 ScreenToWorld(Vector2 screenPos) =>
        mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDist));
}
