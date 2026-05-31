using System.Collections.Generic;
using UnityEngine;

public class RevealPile : MonoBehaviour
{
    public static event System.Action CardDrawnToRevealPile;

    private const int VisibleCount = 3;

    [SerializeField] private Transform pileAnchor;
    [SerializeField] private float spacing = 0.5f;
    [SerializeField] private float margin = 0.3f;

    private readonly List<Card> pileCards = new();
    private LinearCardLayout layout;

    public bool HasCards => pileCards.Count > 0;
    public bool IsCardInPile(Card card) => pileCards.Contains(card);
    public IReadOnlyList<Card> PileCards => pileCards;

    private void Awake()
    {
        layout = new LinearCardLayout(pileAnchor, spacing, margin);
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
            bool visible = i >= count - VisibleCount;
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
        int visibleStart = Mathf.Max(0, count - VisibleCount);
        var visibleCards = new List<Card>(VisibleCount);
        for (int i = visibleStart; i < count; i++)
            visibleCards.Add(pileCards[i]);
        layout.PlaceCards(visibleCards);
    }

    private static void SetCardVisible(Card card, bool visible)
    {
        foreach (Renderer r in card.GetComponentsInChildren<Renderer>(true))
            r.enabled = visible;
        foreach (Canvas c in card.GetComponentsInChildren<Canvas>(true))
            c.enabled = visible;
    }
}
