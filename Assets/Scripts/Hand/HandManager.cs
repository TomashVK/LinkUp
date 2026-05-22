using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Splines;

public class HandManager : MonoBehaviour
{
    [SerializeField] private int maxHandSize;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private CardDeck cardDeck;
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private float spacing = 0.12f;

    private readonly List<Card> handCards = new();
    private float handScrollOffset = 0f;
    private bool isDragging = false;
    private Vector2 lastDragPosition;

    private void OnEnable()
    {
        PointerInputService.Instance.Pressed += OnPointerDown;
        PointerInputService.Instance.Released += OnPointerUp;
        Card.Dropped += OnCardDropped;
        Card.SnapBacked += OnCardSnapBacked;
        Card.DragPickedUp += OnCardDragPickedUp;
    }

    private void OnDisable()
    {
        PointerInputService.Instance.Pressed -= OnPointerDown;
        PointerInputService.Instance.Released -= OnPointerUp;
        Card.Dropped -= OnCardDropped;
        Card.SnapBacked -= OnCardSnapBacked;
        Card.DragPickedUp -= OnCardDragPickedUp;
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

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == cardDeck.gameObject) { DrawCard(); return; }
        }

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

    private void OnCardDragPickedUp(Card draggedCard, Vector3 startPos, Vector3 touchOffset, int restingOrder)
    {
        isDragging = false;

        int index = handCards.IndexOf(draggedCard);
        if (index < 0) return;

        draggedCard.CancelDragSilently();
        draggedCard.BeginDragTransfer(startPos, touchOffset, restingOrder);
    }

    private void DrawCard()
    {
        if (handCards.Count >= maxHandSize) return;
        if (cardDeck == null || !cardDeck.HasCards) return;

        CardData data = cardDeck.DrawNext();
        GameObject newCard = Instantiate(cardPrefab, cardDeck.transform.position, cardDeck.transform.rotation);
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
        float firstCardPosition = 0.5f + handScrollOffset - (cardCount - 1) * spacing / 2f;
        Spline spline = splineContainer.Spline;

        for (int i = 0; i < cardCount; i++)
        {
            bool shouldBeHorizontal = i == cardCount - 1;
            float t = firstCardPosition + i * spacing;
            Vector3 position = spline.EvaluatePosition(t);
            Vector3 forward = spline.EvaluateTangent(t);
            Vector3 up = spline.EvaluateUpVector(t);
            Quaternion rotation = Quaternion.LookRotation(up, Vector3.Cross(up, forward).normalized);

            if (isDragging)
            {
                DOTween.Kill(handCards[i].transform);
                handCards[i].transform.SetPositionAndRotation(position, rotation);
            }
            else
            {
                handCards[i].transform.DOMove(position, 0.25f);
                handCards[i].transform.DORotateQuaternion(rotation, 0.25f);
            }

            handCards[i].SetSortingOrder(i);
            handCards[i].SetHorizontal(shouldBeHorizontal);
        }
    }

    private bool IsHandScrollable()
    {
        if (handCards.Count <= 1) return false;
        return (handCards.Count - 1) * spacing > 1f;
    }

    private float ClampScrollOffset(float offset, int cardCount)
    {
        float halfSpan = (cardCount - 1) * spacing / 2f;
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
        float refT = Mathf.Clamp(0.5f + handScrollOffset, 0f, 1f - dt);

        float screenDist = cam.WorldToScreenPoint(spline.EvaluatePosition(refT + dt)).x
                         - cam.WorldToScreenPoint(spline.EvaluatePosition(refT)).x;

        return Mathf.Abs(screenDist) < 0.001f ? 0f : screenDeltaX * dt / screenDist;
    }
}
