using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Card : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
{    
    private const int dragSortingOrder = 999;

    public static event System.Action<Card> Dropped;
    public static event System.Action<Card> SnapBacked;
    public static event System.Action<Card> DragPickedUp;

    [SerializeField] private float nudgeDuration = 0.5f;
    [SerializeField] private float nudgeAmplitude = 12f;
    [SerializeField] private float nudgeFrequency = 18f;
    [SerializeField] private float nudgeDamping = 7f;
    [SerializeField] private TMP_Text horizontalVisual;
    [SerializeField] private TMP_Text verticalLeftVisual;
    [SerializeField] private TMP_Text verticalRightVisual;
    [SerializeField] private Sprite cardLeftShadow;
    [SerializeField] private Sprite cardRightShadow;

    public int CurrentSortingOrder { get; private set; }
    public bool IsDragging => isDragging;
    public bool IsHorizontal => restingHorizontal;
    public CardData Data { get; private set; }

    private bool isDragging;
    private bool restingHorizontal;
    private bool useRightVertical;
    private Vector2 touchOffset;
    private Vector2 startAnchoredPos;
    private int restingSortingOrder;

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Image image;
    private RectTransform canvasRect;
    private Camera uiCamera;

    private RectTransform CanvasRect
    {
        get
        {
            if (canvasRect != null) return canvasRect;
            Canvas root = canvas != null ? canvas.rootCanvas : GetComponentInParent<Canvas>();
            if (root == null) return null;
            canvasRect = root.GetComponent<RectTransform>();
            uiCamera = root.worldCamera;
            return canvasRect;
        }
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        image = GetComponent<Image>();
        CurrentSortingOrder = canvas != null ? canvas.sortingOrder : 0;
        restingSortingOrder = CurrentSortingOrder;
    }

    private void Start()
    {
        Canvas rootCanvas = canvas != null ? canvas.rootCanvas : GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            canvasRect = rootCanvas.GetComponent<RectTransform>();
            uiCamera = rootCanvas.worldCamera;
        }
    }

    public void Init(CardData data)
    {
        Data = data;
        foreach (TMP_Text label in GetComponentsInChildren<TMP_Text>(true))
            label.text = data.cardName;
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

    public void SetShadowSide(bool useRight)
    {
        if (image == null) return;
        image.sprite = useRight ? cardRightShadow : cardLeftShadow;
    }

    public void SetDraggable(bool draggable)
    {
        if (canvasGroup != null) canvasGroup.blocksRaycasts = draggable;
    }

    public void SetSortingOrder(int order)
    {
        CurrentSortingOrder = order;
        if (canvas != null) canvas.sortingOrder = order;
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isDragging || HandManager.IsAnimating) return;

        startAnchoredPos = rectTransform.anchoredPosition;
        RectTransform parentRT = rectTransform.parent as RectTransform ?? CanvasRect;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, eventData.position, uiCamera, out Vector2 localPos);
        touchOffset = rectTransform.anchoredPosition - localPos;
        restingSortingOrder = CurrentSortingOrder;
        isDragging = true;
        SetSortingOrder(dragSortingOrder);
        DragPickedUp?.Invoke(this);
        ApplyOrientation(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        RectTransform parentRT = rectTransform.parent as RectTransform ?? CanvasRect;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, eventData.position, uiCamera, out Vector2 localPos);
        rectTransform.anchoredPosition = localPos + touchOffset;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;
        HandleDrop(eventData);
    }

    private void HandleDrop(PointerEventData eventData)
    {
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;

        Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(uiCamera, rectTransform.position);
        var pointerData = new PointerEventData(EventSystem.current) { position = screenCenter };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

        Debug.Log($"[Card.HandleDrop] '{Data?.gameId}' dropped. Hits: {results.Count} ({string.Join(", ", System.Array.ConvertAll(results.ToArray(), r => r.gameObject.name))})");

        ICardDrop target = null;
        foreach (var r in results)
            if (r.gameObject.TryGetComponent(out ActiveCardSlot slot)) { target = slot; break; }

        if (target == null)
            foreach (var r in results)
                if (r.gameObject.TryGetComponent(out ICardDrop drop)) { target = drop; break; }

        if (target == null)
        {
            Debug.Log($"[Card.HandleDrop] No ICardDrop target found — snapping back.");
            StartCoroutine(SnapBackWithNudge());
            return;
        }

        bool accepted = target.OnCardDrop(this);
        Debug.Log($"[Card.HandleDrop] Target '{((MonoBehaviour)target).name}' returned accepted={accepted}.");

        if (accepted)
        {
            Dropped?.Invoke(this);
            return;
        }

        StartCoroutine(SnapBackWithNudge());
    }

    private System.Collections.IEnumerator SnapBackWithNudge()
    {
        rectTransform.anchoredPosition = startAnchoredPos;
        SetSortingOrder(restingSortingOrder);
        ApplyOrientation(restingHorizontal);
        yield return StartCoroutine(NudgeCoroutine(startAnchoredPos));
    }

    private System.Collections.IEnumerator NudgeCoroutine(Vector2 snapAnchoredPos)
    {
        float elapsed = 0f;
        while (elapsed < nudgeDuration)
        {
            float nudge = nudgeAmplitude * Mathf.Sin(nudgeFrequency * elapsed) * Mathf.Exp(-nudgeDamping * elapsed);
            rectTransform.anchoredPosition = snapAnchoredPos + Vector2.right * nudge;
            elapsed += Time.deltaTime;
            yield return null;
        }
        rectTransform.anchoredPosition = snapAnchoredPos;
        SnapBacked?.Invoke(this);
    }
}
