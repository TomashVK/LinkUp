using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CardDeck : MonoBehaviour
{
    [SerializeField] private CardData[] cards;
    [SerializeField] private GameObject[] fakeDeckCards;
    [SerializeField] private GameObject emptyStateVisual;
    [SerializeField] private TMP_Text deckCountText;

    private int drawIndex;
    private Vector3 originalPosition;

    public bool HasCards => drawIndex < cards.Length;
    public bool IsEmpty => !HasCards;
    public int RemainingCount => cards.Length - drawIndex;
    public int FakeCardCount => fakeDeckCards != null ? fakeDeckCards.Length : 0;

    private void Awake()
    {
        originalPosition = transform.position;
        if (cards == null || cards.Length == 0)
            cards = CreateFakeDeck();
        RefreshCountText();
    }

    public CardData DrawNext()
    {
        if (!HasCards) return null;
        var data = cards[drawIndex++];
        RefreshCountText();
        return data;
    }

    public void HideTopFakeCard()
    {
        if (fakeDeckCards == null) return;
        foreach (GameObject fake in fakeDeckCards)
            if (fake != null && fake.activeSelf)
            {
                fake.SetActive(false);
                return;
            }
    }

    public Vector3 GetCurrentTopPosition()
    {
        if (fakeDeckCards != null)
            foreach (GameObject fake in fakeDeckCards)
                if (fake != null && fake.activeSelf)
                    return fake.transform.position;
        return transform.position;
    }

    public Vector3 GetSpawnPosition(int remainingBeforeDraw)
    {
        if (fakeDeckCards != null && fakeDeckCards.Length > 0
            && remainingBeforeDraw <= fakeDeckCards.Length)
        {
            int idx = fakeDeckCards.Length - remainingBeforeDraw;
            if (fakeDeckCards[idx] != null)
                return fakeDeckCards[idx].transform.position;
        }
        return transform.position;
    }

    public void OnCardDrawn(int remainingBeforeDraw)
    {
        if (fakeDeckCards != null && fakeDeckCards.Length > 0
            && remainingBeforeDraw <= fakeDeckCards.Length)
        {
            int idx = fakeDeckCards.Length - remainingBeforeDraw;
            if (fakeDeckCards[idx] != null)
                fakeDeckCards[idx].SetActive(false);
        }

        if (!HasCards)
        {
            SetVisible(false);
            if (emptyStateVisual != null) emptyStateVisual.SetActive(true);
        }
    }

    public void RestartDeck()
    {
        drawIndex = 0;
        SetVisible(true);
        if (emptyStateVisual != null) emptyStateVisual.SetActive(false);
        ResetFakeCards();
        transform.position = GetCurrentTopPosition();
        RefreshCountText();
    }

    public void SetCards(List<CardData> newCards)
    {
        cards = newCards.ToArray();
        drawIndex = 0;
        ResetFakeCards();
        if (emptyStateVisual != null) emptyStateVisual.SetActive(false);
        RefreshCountText();
    }

    private void ResetFakeCards()
    {
        if (fakeDeckCards == null) return;
        foreach (GameObject fake in fakeDeckCards)
            if (fake != null) fake.SetActive(true);
    }

    private void RefreshCountText()
    {
        if (deckCountText != null)
            deckCountText.text = RemainingCount.ToString();
    }

    public void SetVisible(bool visible)
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            r.enabled = visible;
        foreach (Canvas c in GetComponentsInChildren<Canvas>(true))
            c.enabled = visible;
    }

    private static CardData[] CreateFakeDeck()
    {
        string[] names = { "Apple", "Banana", "Cherry", "Dog", "Elephant", "Flower", "Guitar", "House", "Ice Cream", "Jellyfish" };
        var deck = new CardData[names.Length];
        for (int i = 0, n = names.Length; i < n; i++)
            deck[i] = new CardData { id = i + 1, cardName = names[i] };
        return deck;
    }
}
