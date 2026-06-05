using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    public static event System.Action CardLeftHand;
    public static event System.Action DeckRestarted;

    [SerializeField] private int maxHandSize;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private CardDeck cardDeck;
    [SerializeField] private float spacing = 1.2f;
    [SerializeField] private float margin = 0.5f;
    [SerializeField] private RevealPile revealPile;
    [SerializeField] private float dealFlipDuration = 0.05f;

    private const float FlipHalfDuration = 0.15f;

    private readonly List<Card> handCards = new();
    private readonly HashSet<Card> draggedFromHand = new();
    private bool isDrawing = false;
    private bool isDealing = false;
    private LinearCardLayout layout;
    private float handScrollOffset;

    public static bool IsAnimating { get; private set; }

    public int CardCount => handCards.Count;
    public IReadOnlyList<Card> HandCards => handCards;

    private void Awake()
    {
        layout = new LinearCardLayout(transform, spacing, margin, centerOnSafeArea: true);
    }

    private void OnEnable()
    {
        PointerInputService.Instance.Pressed += OnPointerDown;
        Card.Dropped += OnCardDropped;
        Card.SnapBacked += OnCardSnapBacked;
        Card.DragPickedUp += OnCardDragPickedUp;
        HandSwipeArea.Scrolled += OnHandScrolled;
    }

    private void OnDisable()
    {
        PointerInputService.Instance.Pressed -= OnPointerDown;
        Card.Dropped -= OnCardDropped;
        Card.SnapBacked -= OnCardSnapBacked;
        Card.DragPickedUp -= OnCardDragPickedUp;
        HandSwipeArea.Scrolled -= OnHandScrolled;
    }

    private void OnPointerDown(Vector2 screenPos)
    {
        if (isDrawing || isDealing) return;
        float zDist = Camera.main.WorldToScreenPoint(cardDeck.transform.position).z;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDist));

        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == cardDeck.gameObject) { DrawCard(); return; }
        }
    }

    private void OnCardDropped(Card card)
    {
        if (!draggedFromHand.Remove(card)) return;
        handCards.Remove(card);
        UpdateCardPositions();
        CardLeftHand?.Invoke();
    }

    private void OnCardSnapBacked(Card card)
    {
        draggedFromHand.Remove(card);
        if (handCards.Contains(card))
            UpdateCardPositions();
    }

    private void OnCardDragPickedUp(Card card)
    {
        if (handCards.Contains(card)) draggedFromHand.Add(card);
    }

    public IEnumerator DealInitial(int handSize, ActiveCardSlot activeSlot)
    {
        isDealing = true;
        yield return StartCoroutine(FlipTopCard(activeSlot.ReceiveCard, dealFlipDuration, dealFlipDuration));
        for (int i = 0; i < handSize; i++)
            yield return StartCoroutine(FlipTopCard(card => {
                handCards.Add(card);
                UpdateCardPositions();
            }, dealFlipDuration, dealFlipDuration));
        isDealing = false;
    }

    public bool CanAcceptCard() => handCards.Count < maxHandSize;

    public void AddCardFromRevealPile(Card card)
    {
        handCards.Add(card);
        UpdateCardPositions();
    }

    private void DrawCard()
    {
        if (isDrawing || isDealing) return;
        if (cardDeck == null) return;
        if (MoveCounter.IsOutOfMoves) return;
        if (!cardDeck.HasCards)
        {
            var pileData = new List<CardData>();
            foreach (Card c in revealPile.PileCards) pileData.Add(c.Data);
            revealPile.ClearPile();
            cardDeck.RestartDeck(pileData);
            DeckRestarted?.Invoke();
            return;
        }
        StartCoroutine(DrawCardToRevealPile());
    }

    private IEnumerator DrawCardToRevealPile()
    {
        yield return StartCoroutine(FlipTopCard(card => revealPile.ReceiveCard(card)));
    }

    private IEnumerator FlipTopCard(System.Action<Card> onFlipped, float halfDuration = FlipHalfDuration, float deckRevealDelay = 0.25f)
    {
        isDrawing = true;
        IsAnimating = true;
        int remainingBeforeDraw = cardDeck.RemainingCount;
        Vector3 spawnPos = cardDeck.GetSpawnPosition();
        CardData data = cardDeck.DrawNext();
        cardDeck.SetCountDisplay(remainingBeforeDraw);
        Vector3 deckEuler = cardDeck.transform.eulerAngles;
        Vector3 deckOriginalScale = cardDeck.transform.localScale;

        cardDeck.transform.position = spawnPos;

        if (remainingBeforeDraw == 1)
            cardDeck.HideDeckVisual();

        GameObject backgroundSpawn = cardDeck.CreateBackgroundSpawn();

        yield return cardDeck.transform
            .DOScaleX(0f, halfDuration)
            .SetEase(Ease.Linear)
            .WaitForCompletion();

        cardDeck.SetVisible(false);
        cardDeck.transform.localScale = deckOriginalScale;

        GameObject newCardObj = Instantiate(cardPrefab, spawnPos, Quaternion.Euler(deckEuler));
        newCardObj.transform.localScale = new Vector3(0f, 1.0f, 1.0f);
        Card card = newCardObj.GetComponent<Card>();
        card.Init(data);
        card.SetHorizontal(true);

        onFlipped?.Invoke(card);
        newCardObj.transform.DOScale(new Vector3(1.0f, 1.0f, 1.0f), halfDuration).SetEase(Ease.Linear);

        yield return new WaitForSeconds(deckRevealDelay);
        if (backgroundSpawn != null) Destroy(backgroundSpawn);
        cardDeck.SetVisible(true);
        cardDeck.UpdateDeckVisual();

        isDrawing = false;
        IsAnimating = false;
    }

    private void OnHandScrolled(float delta)
    {
        layout.ScrollOffset = handScrollOffset + delta;
        layout.PlaceCards(handCards, instant: true);
        handScrollOffset = layout.ScrollOffset; // clamped value written back by PlaceCards
    }

    private void UpdateCardPositions()
    {
        layout.ScrollOffset = handScrollOffset;
        layout.PlaceCards(handCards);
    }
}
