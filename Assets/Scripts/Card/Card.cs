using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Card : MonoBehaviour
{
    public static event System.Action<Card> Dropped;
    public static event System.Action<Card> SnapBacked;
    public static event System.Action<Card, Vector3, Vector3, int> DragPickedUp;

    private const float NudgeDuration = 0.5f;
    private const float NudgeAmplitude = 0.12f;
    private const float NudgeFrequency = 18f;
    private const float NudgeDamping = 7f;
    private const int DragSortingOrder = 999;

    [SerializeField] private TMP_Text horizontalVisual;
    [SerializeField] private TMP_Text verticalVisual;

    public int CurrentSortingOrder { get; private set; }
    public bool IsDragging => isDragging;
    public bool IsHorizontal => restingHorizontal;
    public CardData Data { get; private set; }

    private bool isDragging;
    private bool restingHorizontal;
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

    public void CancelDragSilently()
    {
        isDragging = false;
    }

    public void PlayNudge(Vector3 snapPos, Quaternion snapRot)
    {
        transform.SetPositionAndRotation(snapPos, snapRot);
        StartCoroutine(NudgeCoroutine(snapPos));
    }

    public void BeginDragTransfer(Vector3 startPos, Vector3 offset, int restingOrder)
    {
        startPosition = startPos;
        touchOffset = offset;
        restingSortingOrder = restingOrder;
        isDragging = true;
        SetSortingOrder(DragSortingOrder);
        ApplyOrientation(true);
    }

    public void SetHorizontal(bool isHorizontal)
    {
        restingHorizontal = isHorizontal;
        if (!isDragging) ApplyOrientation(isHorizontal);
    }

    private void ApplyOrientation(bool isHorizontal)
    {
        if (horizontalVisual == null || verticalVisual == null) return;
        horizontalVisual.gameObject.SetActive(isHorizontal);
        verticalVisual.gameObject.SetActive(!isHorizontal);
    }

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
        if (isDragging) return;

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
        touchOffset = transform.position - worldPos;
        restingSortingOrder = CurrentSortingOrder;
        isDragging = true;
        PointerInputService.Instance.IsCardDragging = true;
        SetSortingOrder(DragSortingOrder);
        DragPickedUp?.Invoke(this, startPosition, touchOffset, restingSortingOrder);
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

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out ICardDrop cardDrop))
            {
                cardDrop.OnCardDrop(this);
                Dropped?.Invoke(this);
                return;
            }
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
