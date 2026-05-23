using UnityEngine;

public class CardDeck : MonoBehaviour
{
    [SerializeField] private CardData[] cards;
    [SerializeField] private GameObject[] fakeDeckCards;

    private int drawIndex;

    public bool HasCards => drawIndex < cards.Length;
    public int RemainingCount => cards.Length - drawIndex;

    private void Awake()
    {
        if (cards == null || cards.Length == 0)
            cards = CreateFakeDeck();
    }

    public CardData DrawNext()
    {
        if (!HasCards) return null;
        return cards[drawIndex++];
    }

    // Position of the first still-active fake card; used to rest the deck after each draw.
    public Vector3 GetCurrentTopPosition()
    {
        if (fakeDeckCards != null)
            foreach (GameObject fake in fakeDeckCards)
                if (fake != null && fake.activeSelf)
                    return fake.transform.position;
        return transform.position;
    }

    // Call BEFORE DrawNext() to get the correct spawn position for this draw.
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

    // Call AFTER the flip animation with the same remainingBeforeDraw value.
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
            gameObject.SetActive(false);
    }

    private static CardData[] CreateFakeDeck()
    {
        string[] names = { "Apple", "Banana", "Cherry", "Dog", "Elephant"};
        var deck = new CardData[names.Length];
        for (int i = 0, n = names.Length; i < n; i++)
            deck[i] = new CardData { id = i + 1, cardName = names[i] };
        return deck;
    }
}
