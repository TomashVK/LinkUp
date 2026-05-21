using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Splines;

public class HandManager : MonoBehaviour
{
    [SerializeField] private int maxHandSize;
    [SerializeField] private GameObject horizontalCardPrefab;
    [SerializeField] private GameObject verticalCardPrefab;
    [SerializeField] private CardDeck cardDeck;
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField][Range(0.02f, 0.2f)] private float maxSpacing = 0.12f;

    public static GameObject HorizontalCardPrefab { get; private set; }

    private readonly List<Card> handCards = new();
    private float handScrollOffset = 0f;
    private bool isDragging = false;
    private Vector2 lastDragPosition;

    private void OnEnable()
    {
        HorizontalCardPrefab = horizontalCardPrefab;
        PointerInputService.Instance.Pressed += OnPointerDown;
        PointerInputService.Instance.Released += OnPointerUp;
        Card.Dropped += OnCardDropped;
        Card.SnapBacked += OnCardSnapBacked;
        Card.DragPickedUp += OnCardDragPickedUp;
        Card.SnapBackStarted += OnCardSnapBackStarted;
    }

    private void OnDisable()
    {
        PointerInputService.Instance.Pressed -= OnPointerDown;
        PointerInputService.Instance.Released -= OnPointerUp;
        Card.Dropped -= OnCardDropped;
        Card.SnapBacked -= OnCardSnapBacked;
        Card.DragPickedUp -= OnCardDragPickedUp;
        Card.SnapBackStarted -= OnCardSnapBackStarted;
    }

    private void Update()
    {
        HandleHandDrag();
    }

    private void OnPointerDown(Vector2 screenPos)
    {
        float zDist = Camera.main.WorldToScreenPoint(cardDeck.transform.position).z;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDist));

        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);

        bool hitDeck = false;
        bool hitCard = false;
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == cardDeck.gameObject) hitDeck = true;
            if (hit.TryGetComponent<Card>(out _)) hitCard = true;
        }

        if (hitDeck) { DrawCard(); return; }
        if (hitCard) return;

        isDragging = true;
        lastDragPosition = screenPos;
    }

    private void OnPointerUp(Vector2 screenPos)
    {
        isDragging = false;
    }

    private void OnCardDropped(Card card)
    {
        handCards.Remove(card);
        if (handCards.Count == 0) handScrollOffset = 0f;
        UpdateCardPositions();
    }

    private void OnCardSnapBacked(Card card)
    {
        if (handCards.Contains(card))
            UpdateCardPositions();
    }

    private void OnCardSnapBackStarted(Card snapCard, Vector3 snapPos, Quaternion snapRot)
    {
        int index = handCards.IndexOf(snapCard);
        if (index < 0) return;

        bool shouldBeHorizontal = index == handCards.Count - 1;
        if (snapCard.IsHorizontal == shouldBeHorizontal) return;

        snapCard.MarkAsReplaced();
        CardData data = snapCard.Data;
        DOTween.Kill(snapCard.transform);
        Destroy(snapCard.gameObject);

        GameObject prefab = shouldBeHorizontal ? horizontalCardPrefab : verticalCardPrefab;
        Card newCard = Instantiate(prefab, snapPos, snapRot).GetComponent<Card>();
        newCard.Init(data);
        newCard.SetHorizontal(shouldBeHorizontal);
        newCard.SetSortingOrder(index);
        handCards[index] = newCard;
        newCard.PlayNudge(snapPos, snapRot);
    }

    private void OnCardDragPickedUp(Card draggedCard, Vector3 startPos, Vector3 touchOffset, int restingOrder)
    {
        int index = handCards.IndexOf(draggedCard);
        if (index < 0 || draggedCard.IsHorizontal) return;

        draggedCard.CancelDragSilently();
        CardData data = draggedCard.Data;
        draggedCard.transform.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);
        DOTween.Kill(draggedCard.transform);
        Destroy(draggedCard.gameObject);

        Card newCard = Instantiate(horizontalCardPrefab, pos, rot).GetComponent<Card>();
        newCard.Init(data);
        newCard.SetHorizontal(true);
        handCards[index] = newCard;
        newCard.BeginDragTransfer(startPos, touchOffset, restingOrder);
    }

    private void DrawCard()
    {
        if (handCards.Count >= maxHandSize) return;
        if (cardDeck == null || !cardDeck.HasCards) return;

        CardData data = cardDeck.DrawNext();
        GameObject prefab = horizontalCardPrefab;
        GameObject newCard = Instantiate(prefab, cardDeck.transform.position, cardDeck.transform.rotation);
        Card card = newCard.GetComponent<Card>();
        card.Init(data);
        handCards.Add(card);

        UpdateCardPositions();
    }

    private void UpdateCardPositions()
    {
        int cardCount = handCards.Count;
        if (cardCount == 0) return;

        if (!IsHandScrollable()) handScrollOffset = 0f;
        handScrollOffset = ClampScrollOffset(handScrollOffset, cardCount);
        float spacing = CalculateSpacing(cardCount);
        float firstCardPosition = 0.5f + handScrollOffset - (cardCount - 1) * spacing / 2f;
        Spline spline = splineContainer.Spline;

        for (int i = 0; i < cardCount; i++)
        {
            bool shouldBeHorizontal = i == cardCount - 1;
            if (handCards[i].IsHorizontal != shouldBeHorizontal)
                SwapCard(i, shouldBeHorizontal);

            float t = firstCardPosition + i * spacing;
            Vector3 position = spline.EvaluatePosition(t);
            Vector3 forward = spline.EvaluateTangent(t);
            Vector3 up = spline.EvaluateUpVector(t);
            Quaternion rotation = Quaternion.LookRotation(up, Vector3.Cross(up, forward).normalized);
            handCards[i].transform.DOMove(position, 0.25f);
            handCards[i].transform.DORotateQuaternion(rotation, 0.25f);
            handCards[i].SetSortingOrder(i);
            handCards[i].SetHorizontal(shouldBeHorizontal);
        }
    }

    private void SwapCard(int index, bool toHorizontal)
    {
        Card oldCard = handCards[index];
        CardData data = oldCard.Data;
        oldCard.transform.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);

        DOTween.Kill(oldCard.transform);
        Destroy(oldCard.gameObject);

        GameObject prefab = toHorizontal ? horizontalCardPrefab : verticalCardPrefab;
        Card newCard = Instantiate(prefab, pos, rot).GetComponent<Card>();
        newCard.Init(data);
        handCards[index] = newCard;
    }

    private float CalculateBaseSpacing(int cardCount) => maxSpacing;

    private float CalculateSpacing(int cardCount) => maxSpacing;

    private bool IsHandScrollable()
    {
        if (handCards.Count <= 1) return false;
        return (handCards.Count - 1) * maxSpacing > 1f;
    }

    private float ClampScrollOffset(float offset, int cardCount)
    {
        float halfSpan = (cardCount - 1) * CalculateBaseSpacing(cardCount) / 2f;
        float minOffset = halfSpan - 0.5f;
        float maxOffset = 0.5f - halfSpan;
        return minOffset <= maxOffset
            ? Mathf.Clamp(offset, minOffset, maxOffset)
            : Mathf.Clamp(offset, maxOffset, minOffset);
    }

    private void HandleHandDrag()
    {
        if (!isDragging || handCards.Count == 0) return;
        if (!IsHandScrollable()) return;

        Vector2 currentPos = PointerInputService.Instance.Position;
        float deltaX = currentPos.x - lastDragPosition.x;
        lastDragPosition = currentPos;

        if (Mathf.Abs(deltaX) < 0.001f) return;

        handScrollOffset = ClampScrollOffset(handScrollOffset + ScreenDeltaToSplineDelta(deltaX), handCards.Count);
        UpdateCardPositions();
    }

    private float ScreenDeltaToSplineDelta(float screenDeltaX)
    {
        Camera cam = Camera.main;
        if (cam == null) return 0f;

        Spline spline = splineContainer.Spline;
        const float dt = 0.01f;
        float refT = Mathf.Clamp01(0.5f + handScrollOffset);

        float screenDist = cam.WorldToScreenPoint(spline.EvaluatePosition(Mathf.Clamp01(refT + dt))).x
                         - cam.WorldToScreenPoint(spline.EvaluatePosition(refT)).x;

        return Mathf.Abs(screenDist) < 0.001f ? 0f : screenDeltaX * dt / screenDist;
    }
}
