using System.Collections.Generic;
using UnityEngine;

public class RevealPile : MonoBehaviour
{
    public static event System.Action CardDrawnToRevealPile;

    [SerializeField] private int visibleCount = 3;
    [SerializeField] private float spacing = 80f;

    private readonly List<Card> pileCards = new();
    private LinearCardLayout layout;

    public bool HasCards => pileCards.Count > 0;
    public bool IsCardInPile(Card card) => pileCards.Contains(card);
    public IReadOnlyList<Card> PileCards => pileCards;

    private void Awake()
    {
        layout = new LinearCardLayout(transform, spacing, rightAnchored: true)
        {
            Mirrored = true,
            UseVerticalRight = true
        };
    }

    private void OnEnable()
    {
        Card.Dropped += OnCardDropped;
        Card.SnapBacked += OnCardSnapBacked;
    }

    private void OnDisable()
    {
        Card.Dropped -= OnCardDropped;
        Card.SnapBacked -= OnCardSnapBacked;
    }

    public void ClearPile()
    {
        foreach (Card card in pileCards)
            if (card != null) Destroy(card.gameObject);
        pileCards.Clear();
    }

    public void ReceiveCard(Card card)
    {
        pileCards.Add(card);
        card.SetShadowSide(true);
        RefreshVisibility();
        UpdateDraggability();
        UpdateCardPositions();
        CardDrawnToRevealPile?.Invoke();
    }

    private void OnCardDropped(Card card)
    {
        if (!pileCards.Remove(card)) return;
        RefreshVisibility();
        UpdateDraggability();
        UpdateCardPositions();
    }

    private void OnCardSnapBacked(Card card)
    {
        if (pileCards.Contains(card))
            UpdateCardPositions();
    }

    private void RefreshVisibility()
    {
        int count = pileCards.Count;
        for (int i = 0; i < count; i++)
        {
            bool visible = i >= count - visibleCount;
            SetCardVisible(pileCards[i], visible);
        }
    }

    private void UpdateDraggability()
    {
        int count = pileCards.Count;
        for (int i = 0; i < count; i++)
            pileCards[i].SetDraggable(i == count - 1);
    }

    private void UpdateCardPositions()
    {
        int count = pileCards.Count;
        int visibleStart = Mathf.Max(0, count - visibleCount);
        var visibleCards = new List<Card>(visibleCount);
        for (int i = visibleStart; i < count; i++)
            visibleCards.Add(pileCards[i]);
        layout.PlaceCards(visibleCards);
    }

    private static void SetCardVisible(Card card, bool visible)
    {
        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        if (cg == null) return;
        cg.alpha = visible ? 1f : 0f;
        cg.blocksRaycasts = visible;
    }
}
