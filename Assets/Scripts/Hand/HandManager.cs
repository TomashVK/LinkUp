using System.Collections;
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

    private const float FlipHalfDuration = 0.15f;

    private readonly List<Card> handCards = new();
    private bool isDragging = false;
    private bool isDrawing = false;
    private Vector2 lastDragPosition;
    private HandSplineLayout _layout;

    private void Awake()
    {
        _layout = new HandSplineLayout(splineContainer, spacing);
    }

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
        if (handCards.Count == 0) _layout.ScrollOffset = 0f;
        UpdateCardPositions();
    }

    private void OnCardSnapBacked(Card card)
    {
        if (handCards.Contains(card))
            UpdateCardPositions();
    }

    private void OnCardDragPickedUp(Card draggedCard)
    {
        isDragging = false;
    }

    private void DrawCard()
    {
        if (isDrawing || handCards.Count >= maxHandSize) return;
        if (cardDeck == null || !cardDeck.HasCards) return;
        StartCoroutine(DrawCardWithFlip());
    }

    private IEnumerator DrawCardWithFlip()
    {
        isDrawing = true;
        int remainingBeforeDraw = cardDeck.RemainingCount;
        Vector3 spawnPos = cardDeck.GetSpawnPosition(remainingBeforeDraw);
        CardData data = cardDeck.DrawNext();
        Vector3 deckEuler = cardDeck.transform.eulerAngles;

        // Move deck to the fake card's position — it stays there after the draw (deck sinks into the stack).
        cardDeck.transform.position = spawnPos;

        // On the last draw, destroy the fake card underneath before the animation so it never shows through.
        if (remainingBeforeDraw == 1)
            cardDeck.HideTopFakeCard();

        // Phase 1: rotate deck to edge-on (Y + 90)
        yield return cardDeck.transform
            .DORotate(new Vector3(deckEuler.x, deckEuler.y + 90f, deckEuler.z), FlipHalfDuration)
            .WaitForCompletion();

        // Hide deck and snap back to original rotation while invisible
        cardDeck.SetVisible(false);
        cardDeck.transform.rotation = Quaternion.Euler(deckEuler);

        // Spawn card edge-on at the top fake card's position
        Quaternion cardStartRot = Quaternion.Euler(deckEuler.x, deckEuler.y + 90f, deckEuler.z);
        GameObject newCardObj = Instantiate(cardPrefab, spawnPos, cardStartRot);
        Card card = newCardObj.GetComponent<Card>();
        card.Init(data);
        card.SetHorizontal(true);

        // Phase 2: rotate card to front face
        yield return newCardObj.transform
            .DORotate(deckEuler, FlipHalfDuration)
            .WaitForCompletion();

        cardDeck.SetVisible(true);
        cardDeck.OnCardDrawn(remainingBeforeDraw);
        if (cardDeck.gameObject.activeSelf && remainingBeforeDraw <= cardDeck.FakeCardCount + 1)
        {
            cardDeck.transform.position = cardDeck.GetCurrentTopPosition();
            cardDeck.HideTopFakeCard();
        }
        handCards.Add(card);
        isDrawing = false;
        UpdateCardPositions();
    }

    private void UpdateCardPositions()
    {
        _layout.PlaceCards(handCards, isDragging);
    }

    private void HandleHandDrag()
    {
        if (!isDragging || handCards.Count == 0) return;
        if (!_layout.IsScrollable(handCards.Count)) return;

        Vector2 currentPos = PointerInputService.Instance.Position;
        float deltaX = currentPos.x - lastDragPosition.x;
        lastDragPosition = currentPos;

        if (Mathf.Abs(deltaX) < 0.001f) return;

        _layout.ScrollOffset = _layout.ClampOffset(
            _layout.ScrollOffset + _layout.ScreenDeltaToSplineDelta(deltaX),
            handCards.Count);
        UpdateCardPositions();
    }
}
