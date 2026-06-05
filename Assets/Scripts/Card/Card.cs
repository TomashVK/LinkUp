using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Card : MonoBehaviour
{
    public static event System.Action<Card> Dropped;
    public static event System.Action<Card> SnapBacked;
    public static event System.Action<Card> DragPickedUp;

    private const float NudgeDuration = 0.5f;
    private const float NudgeAmplitude = 0.12f;
    private const float NudgeFrequency = 18f;
    private const float NudgeDamping = 7f;
    private const int DragSortingOrder = 999;

    [SerializeField] private TMP_Text horizontalVisual;
    [SerializeField] private TMP_Text verticalLeftVisual;
    [SerializeField] private TMP_Text verticalRightVisual;


    public int CurrentSortingOrder { get; private set; }
    public bool IsDragging => isDragging;
    public bool IsHorizontal => restingHorizontal;
    public CardData Data { get; private set; }

    private bool isDragging;
    private bool restingHorizontal;
    private bool useRightVertical;
    private Vector3 touchOffset;
    private Vector3 startPosition;
    private Collider2D cardCollider;
    private Camera mainCamera;
    private float zDistance;
    private int restingSortingOrder;

    private void Awake()
    {
        mainCamera = Camera.main;
        cardCollider = GetComponent<Collider2D>();
        SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>(true);
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        CurrentSortingOrder = srs.Length > 0 ? srs[0].sortingOrder :
                              canvases.Length > 0 ? canvases[0].sortingOrder : 0;
        restingSortingOrder = CurrentSortingOrder;
    }

    public void Init(CardData data)
    {
        Data = data;
        foreach (TMP_Text label in GetComponentsInChildren<TMP_Text>(true))
            label.text = data.cardName;
    }

    public void PlayNudge(Vector3 snapPos, Quaternion snapRot)
    {
        transform.SetPositionAndRotation(snapPos, snapRot);
        StartCoroutine(NudgeCoroutine(snapPos));
    }

    public void PreRotateCanvasForFlip()
    {
        foreach (Canvas c in GetComponentsInChildren<Canvas>(true))
            c.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
    }

    public void ResetCanvasRotation()
    {
        foreach (Canvas c in GetComponentsInChildren<Canvas>(true))
            c.transform.localRotation = Quaternion.identity;
    }

    public void SetHorizontal(bool isHorizontal)
    {
        restingHorizontal = isHorizontal;
        if (!isDragging) ApplyOrientation(isHorizontal);
    }

    public void SetVerticalRight(bool useRight)
    {
        useRightVertical = useRight;
        if (!isDragging && !restingHorizontal) ApplyOrientation(false);
    }

    private void ApplyOrientation(bool isHorizontal)
    {
        if (horizontalVisual != null) horizontalVisual.gameObject.SetActive(isHorizontal);
        if (verticalLeftVisual != null) verticalLeftVisual.gameObject.SetActive(!isHorizontal && !useRightVertical);
        if (verticalRightVisual != null) verticalRightVisual.gameObject.SetActive(!isHorizontal && useRightVertical);
    }

    public void SetInteractable(bool interactable) => cardCollider.enabled = interactable;
    public void SetDraggable(bool draggable) => cardCollider.enabled = draggable;

    public void SetSortingOrder(int order)
    {
        CurrentSortingOrder = order;
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
            sr.sortingOrder = order;
        foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true))
            canvas.sortingOrder = order;
    }

    private void Start()
    {
        zDistance = mainCamera.WorldToScreenPoint(transform.position).z;
    }

    private void OnEnable()
    {
        PointerInputService.Instance.Pressed += OnPointerDown;
        PointerInputService.Instance.Released += OnPointerUp;
    }

    private void OnDisable()
    {
        PointerInputService.Instance.Pressed -= OnPointerDown;
        PointerInputService.Instance.Released -= OnPointerUp;
    }

    private void LateUpdate()
    {
        if (!isDragging) return;
        transform.position = ScreenToWorld(PointerInputService.Instance.Position) + touchOffset;
    }

    private void OnPointerDown(Vector2 screenPos)
    {
        if (isDragging || HandManager.IsAnimating) return;

        Vector3 worldPos = ScreenToWorld(screenPos);
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);

        Card topCard = null;
        int highestOrder = int.MinValue;
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<Card>(out var candidate) && candidate.CurrentSortingOrder > highestOrder)
            {
                highestOrder = candidate.CurrentSortingOrder;
                topCard = candidate;
            }
        }

        if (topCard != this) return;

        startPosition = transform.position;
        touchOffset = new Vector3(transform.position.x - worldPos.x, transform.position.y - worldPos.y, 0f);
        restingSortingOrder = CurrentSortingOrder;
        isDragging = true;
        PointerInputService.Instance.IsCardDragging = true;
        SetSortingOrder(DragSortingOrder);
        DragPickedUp?.Invoke(this);
        ApplyOrientation(true);
    }

    private void OnPointerUp(Vector2 screenPos)
    {
        if (!isDragging) return;
        isDragging = false;
        PointerInputService.Instance.IsCardDragging = false;
        HandleDrop();
    }

    private void HandleDrop()
    {
        cardCollider.enabled = false;
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);
        cardCollider.enabled = true;

        Debug.Log($"[Card.HandleDrop] '{Data?.gameId}' dropped at {transform.position}. Hits: {hits.Length} ({string.Join(", ", System.Array.ConvertAll(hits, h => h.name))})");

        // Try ActiveCardSlot first, then fall back to any other ICardDrop.
        ICardDrop target = null;
        foreach (Collider2D hit in hits)
            if (hit.TryGetComponent(out ActiveCardSlot slot)) { target = slot; break; }

        if (target == null)
            foreach (Collider2D hit in hits)
                if (hit.TryGetComponent(out ICardDrop drop)) { target = drop; break; }

        if (target == null)
        {
            Debug.Log($"[Card.HandleDrop] No ICardDrop target found — snapping back.");
            StartCoroutine(SnapBackWithNudge());
            return;
        }

        bool accepted = target.OnCardDrop(this);
        Debug.Log($"[Card.HandleDrop] Target '{((MonoBehaviour)target).name}' returned accepted={accepted}. Card pos after={transform.position}, sortingOrder={CurrentSortingOrder}");

        if (accepted)
        {
            Dropped?.Invoke(this);
            return;
        }

        StartCoroutine(SnapBackWithNudge());
    }

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        return mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDistance));
    }

    private System.Collections.IEnumerator SnapBackWithNudge()
    {
        transform.position = startPosition;
        SetSortingOrder(restingSortingOrder);
        ApplyOrientation(restingHorizontal);
        yield return StartCoroutine(NudgeCoroutine(startPosition));
    }

    private System.Collections.IEnumerator NudgeCoroutine(Vector3 snapPos)
    {
        float elapsed = 0f;
        while (elapsed < NudgeDuration)
        {
            float nudge = NudgeAmplitude * Mathf.Sin(NudgeFrequency * elapsed) * Mathf.Exp(-NudgeDamping * elapsed);
            transform.position = snapPos + Vector3.right * nudge;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = snapPos;
        SnapBacked?.Invoke(this);
    }
}
