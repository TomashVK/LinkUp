using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Solo.MOST_IN_ONE;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    public static event System.Action CardLeftHand;
    public static event System.Action DeckRestarted;
    public static event System.Action<IReadOnlyList<Card>> BeforeDeckRestart;

    [SerializeField] private int maxHandSize;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private CardDeck cardDeck;
    [SerializeField] private float cardSpacing = 80f;
    [SerializeField] private float margin = 50f;
    [SerializeField] private RevealPile revealPile;
    [SerializeField] private RectTransform cardContainer;

    private readonly List<Card> handCards = new();
    private readonly HashSet<Card> draggedFromHand = new();
    private bool isDrawing = false;
    private bool isDealing = false;
    private LinearCardLayout layout;
    private float handScrollOffset;

    public static bool IsAnimating { get; set; }

    public int CardCount => handCards.Count;
    public RectTransform CardContainer => cardContainer;

    private void Awake()
    {
        layout = new LinearCardLayout(transform, cardSpacing, margin, centerOnSafeArea: true);
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
            StartCoroutine(DealCard(card =>
            {
                handCards.Add(card);
                UpdateCardPositions();
            }, (captured + 1) * CardAnimationSettings.Instance.DealStagger, () => completed++));
        }

        yield return new WaitUntil(() => completed >= total);
        isDealing = false;
        IsAnimating = false;
    }

    private IEnumerator DealCard(System.Action<Card> onPlaced, float delay, System.Action onDone)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        int countBeforeDraw = cardDeck.RemainingCount;
        CardData data = cardDeck.DrawNext();
        cardDeck.UpdateDeckVisual();

        RectTransform container = cardContainer != null ? cardContainer : transform.root.GetComponent<RectTransform>();
        Vector2 spawnPos = cardDeck.GetSpawnPosition(container);
        GameObject newCardObj = Instantiate(cardPrefab, container);
        RectTransform newCardRT = newCardObj.GetComponent<RectTransform>();
        newCardRT.anchoredPosition = spawnPos;
        newCardRT.localEulerAngles = cardDeck.transform.localEulerAngles;

        Card card = newCardObj.GetComponent<Card>();
        card.SetHorizontal(true);
        card.ShowBack(countBeforeDraw);

        onPlaced?.Invoke(card);

        yield return new WaitForSeconds(CardAnimationSettings.Instance.MoveDuration * CardAnimationSettings.Instance.FlipStartPercent);
        yield return newCardObj.transform.DOScaleX(0f, CardAnimationSettings.Instance.FlipHalfDuration).SetEase(Ease.Linear).WaitForCompletion();

        card.HideBack();
        card.Init(data);

        yield return newCardObj.transform.DOScaleX(1f, CardAnimationSettings.Instance.FlipHalfDuration).SetEase(Ease.Linear).WaitForCompletion();

        onDone?.Invoke();
    }

    public bool CanAcceptCard() => handCards.Count < maxHandSize;

    public void AddCardFromRevealPile(Card card)
    {
        handCards.Add(card);
        UpdateCardPositions();
    }

    public void RemoveCardFromHand(Card card)
    {
        handCards.Remove(card);
        UpdateCardPositions();
    }

    public void InsertCardAtHand(Card card, int index)
    {
        index = Mathf.Clamp(index, 0, handCards.Count);
        handCards.Insert(index, card);
        UpdateCardPositions();
    }

    public int IndexOfCard(Card card) => handCards.IndexOf(card);

    public Card CreateCardFromData(CardData data, Vector2 spawnPos)
    {
        RectTransform container = cardContainer != null ? cardContainer : transform.root.GetComponent<RectTransform>();
        GameObject newCardObj = Instantiate(cardPrefab, container);
        newCardObj.GetComponent<RectTransform>().anchoredPosition = spawnPos;
        Card card = newCardObj.GetComponent<Card>();
        card.Init(data);
        card.SetHorizontal(true);
        return card;
    }

    private void DrawCard()
    {
        MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.MediumImpact);
        if (isDrawing || isDealing) return;
        if (cardDeck == null) return;
        if (MoveCounter.IsOutOfMoves) return;
        if (!cardDeck.HasCards)
        {
            BeforeDeckRestart?.Invoke(revealPile.PileCards);
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

        int countBeforeDraw = cardDeck.RemainingCount;
        CardData data = cardDeck.DrawNext();
        cardDeck.UpdateDeckVisual();

        RectTransform container = cardContainer != null ? cardContainer : transform.root.GetComponent<RectTransform>();
        Vector2 spawnPos = cardDeck.GetSpawnPosition(container);
        GameObject newCardObj = Instantiate(cardPrefab, container);
        RectTransform newCardRT = newCardObj.GetComponent<RectTransform>();
        newCardRT.anchoredPosition = spawnPos;
        newCardRT.localEulerAngles = cardDeck.transform.localEulerAngles;

        Card card = newCardObj.GetComponent<Card>();
        card.SetHorizontal(true);
        card.ShowBack(countBeforeDraw);

        onPlaced?.Invoke(card);

        yield return new WaitForSeconds(CardAnimationSettings.Instance.MoveDuration * CardAnimationSettings.Instance.FlipStartPercent);

        yield return newCardObj.transform.DOScaleX(0f, CardAnimationSettings.Instance.FlipHalfDuration).SetEase(Ease.Linear).WaitForCompletion();

        // Swap: reveal card face (same as original mid-flip reveal)
        card.HideBack();
        card.Init(data);
        UndoManager.Instance?.RecordDraw(card);

        // Second half of flip — card face unfolds into view
        yield return newCardObj.transform.DOScaleX(1f, CardAnimationSettings.Instance.FlipHalfDuration).SetEase(Ease.Linear).WaitForCompletion();

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
