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
    [SerializeField] private float spacing = 160f;
    [SerializeField] private float margin = 50f;
    [SerializeField] private RevealPile revealPile;
    [SerializeField] private RectTransform cardContainer;
    [SerializeField] private float flipHalfDuration = 0.15f;
    [SerializeField] private float dealStagger = 0.12f;

    private readonly List<Card> handCards = new();
    private readonly HashSet<Card> draggedFromHand = new();
    private bool isDrawing = false;
    private bool isDealing = false;
    private LinearCardLayout layout;
    private float handScrollOffset;

    public static bool IsAnimating { get; private set; }

    public int CardCount => handCards.Count;

    private void Awake()
    {
        layout = new LinearCardLayout(transform, spacing, margin, centerOnSafeArea: true);
    }

    private void OnEnable()
    {
        CardDeck.Tapped += DrawCard;
        Card.Dropped += OnCardDropped;
        Card.SnapBacked += OnCardSnapBacked;
        Card.DragPickedUp += OnCardDragPickedUp;
        HandSwipeArea.Scrolled += OnHandScrolled;
    }

    private void OnDisable()
    {
        CardDeck.Tapped -= DrawCard;
        Card.Dropped -= OnCardDropped;
        Card.SnapBacked -= OnCardSnapBacked;
        Card.DragPickedUp -= OnCardDragPickedUp;
        HandSwipeArea.Scrolled -= OnHandScrolled;
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
        IsAnimating = true;

        int total = 1 + handSize;
        int completed = 0;

        StartCoroutine(DealCard(activeSlot.ReceiveCard, 0f, () => completed++));
        for (int i = 0; i < handSize; i++)
        {
            int captured = i;
            StartCoroutine(DealCard(card => {
                handCards.Add(card);
                UpdateCardPositions();
            }, (captured + 1) * dealStagger, () => completed++));
        }

        yield return new WaitUntil(() => completed >= total);
        isDealing = false;
        IsAnimating = false;
    }

    private IEnumerator DealCard(System.Action<Card> onPlaced, float delay, System.Action onDone)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        Vector2 spawnPos = cardDeck.GetSpawnPosition();
        int countBeforeDraw = cardDeck.RemainingCount;
        CardData data = cardDeck.DrawNext();
        cardDeck.UpdateDeckVisual();

        RectTransform container = cardContainer != null ? cardContainer : transform.root.GetComponent<RectTransform>();
        GameObject newCardObj = Instantiate(cardPrefab, container);
        RectTransform newCardRT = newCardObj.GetComponent<RectTransform>();
        newCardRT.anchoredPosition = spawnPos;
        newCardRT.localEulerAngles = cardDeck.transform.localEulerAngles;

        GameObject backVisualObj = cardDeck.CreateTravelingVisual(newCardObj.transform, countBeforeDraw);

        Card card = newCardObj.GetComponent<Card>();
        card.SetHorizontal(true);

        float moveDuration = 0.25f;
        onPlaced?.Invoke(card);

        yield return new WaitForSeconds(moveDuration * 0.9f);
        yield return newCardObj.transform.DOScaleX(0f, flipHalfDuration).SetEase(Ease.Linear).WaitForCompletion();

        if (backVisualObj != null) Destroy(backVisualObj);
        card.Init(data);

        yield return newCardObj.transform.DOScaleX(1f, flipHalfDuration).SetEase(Ease.Linear).WaitForCompletion();

        onDone?.Invoke();
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
        yield return StartCoroutine(DrawTopCard(card => revealPile.ReceiveCard(card)));
    }

    private IEnumerator DrawTopCard(System.Action<Card> onPlaced)
    {
        isDrawing = true;
        IsAnimating = true;

        Vector2 spawnPos = cardDeck.GetSpawnPosition();
        int countBeforeDraw = cardDeck.RemainingCount;
        CardData data = cardDeck.DrawNext();
        cardDeck.UpdateDeckVisual();

        RectTransform container = cardContainer != null ? cardContainer : transform.root.GetComponent<RectTransform>();
        GameObject newCardObj = Instantiate(cardPrefab, container);
        RectTransform newCardRT = newCardObj.GetComponent<RectTransform>();
        newCardRT.anchoredPosition = spawnPos;
        newCardRT.localEulerAngles = cardDeck.transform.localEulerAngles;

        // Overlay back-face visual (sprite + counter) as a child so it travels with the card
        GameObject backVisualObj = cardDeck.CreateTravelingVisual(newCardObj.transform, countBeforeDraw);

        Card card = newCardObj.GetComponent<Card>();
        card.SetHorizontal(true);

        float moveDuration = 0.25f;
        onPlaced?.Invoke(card);

        // Wait until close to target (90% of move)
        yield return new WaitForSeconds(moveDuration * 0.9f);

        // First half of flip — back face folds away
        yield return newCardObj.transform.DOScaleX(0f, flipHalfDuration).SetEase(Ease.Linear).WaitForCompletion();

        // Swap: remove back face, reveal card prefab (same as original mid-flip reveal)
        if (backVisualObj != null) Destroy(backVisualObj);
        card.Init(data);

        // Second half of flip — card face unfolds into view
        yield return newCardObj.transform.DOScaleX(1f, flipHalfDuration).SetEase(Ease.Linear).WaitForCompletion();

        isDrawing = false;
        IsAnimating = false;
    }

    private void OnHandScrolled(float delta)
    {
        layout.ScrollOffset = handScrollOffset + delta;
        layout.PlaceCards(handCards, instant: true);
        handScrollOffset = layout.ScrollOffset;
    }

    private void UpdateCardPositions()
    {
        layout.ScrollOffset = handScrollOffset;
        layout.PlaceCards(handCards);
    }
}
