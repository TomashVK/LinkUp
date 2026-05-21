using UnityEngine;

public class CardDeck : MonoBehaviour
{
    [SerializeField] private CardData[] cards;

    private int drawIndex;

    public bool HasCards => drawIndex < cards.Length;

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

    private static CardData[] CreateFakeDeck()
    {
        string[] names = { "Apple", "Banana", "Cherry", "Dog", "Elephant", "Frog", "Guitar", "House", "Island", "Jungle", "Kite", "Lemon", "Apple", "Banana", "Cherry", "Dog", "Elephant", "Frog", "Guitar", "House", "Island", "Jungle", "Kite", "Lemon", "Apple", "Banana", "Cherry", "Dog", "Elephant", "Frog", "Guitar", "House", "Island", "Jungle", "Kite", "Lemon" };
        var deck = new CardData[names.Length];
        for (int i = 0; i < names.Length; i++)
            deck[i] = new CardData { id = i + 1, cardName = names[i] };
        return deck;
    }
}

