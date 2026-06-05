using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CardDeck : MonoBehaviour
{
    [SerializeField] private CardData[] cards;
    [SerializeField] private SpriteRenderer deckVisual;
    [SerializeField] private Sprite[] deckVisualSprites;
    [SerializeField] private GameObject emptyStateVisual;
    [SerializeField] private TMP_Text deckCountText;
    [SerializeField] private Transform spawnContainer;

    private int drawIndex;
    private CardData[] originalCards;

    public bool HasCards => drawIndex < cards.Length;
    public bool IsEmpty => !HasCards;
    public int RemainingCount => cards.Length - drawIndex;

    private void Awake()
    {
        if (cards == null || cards.Length == 0)
            cards = CreateFakeDeck();
        originalCards = cards;
        RefreshCountText();
    }

    public CardData DrawNext()
    {
        if (!HasCards) return null;
        var data = cards[drawIndex++];
        RefreshCountText();
        return data;
    }

    public void HideDeckVisual()
    {
        if (deckVisual != null)
            deckVisual.gameObject.SetActive(false);
    }

    public Vector3 GetSpawnPosition()
    {
        if (deckVisual != null && deckVisualSprites != null && deckVisualSprites.Length > 0 && deckVisualSprites[0] != null)
        {
            Bounds b = deckVisual.bounds;
            float singleCardHalfH = deckVisualSprites[0].bounds.extents.y * deckVisual.transform.lossyScale.y;
            return new Vector3(b.center.x, b.max.y - singleCardHalfH, b.center.z);
        }
        if (deckVisual != null)
            return deckVisual.transform.position;
        return transform.position;
    }

    public void UpdateDeckVisual()
    {
        int remaining = RemainingCount;
        bool hasCards = remaining > 0;

        if (deckVisual != null)
        {
            deckVisual.gameObject.SetActive(hasCards);
            if (hasCards && deckVisualSprites != null && deckVisualSprites.Length > 0)
            {
                int level = Mathf.Clamp(remaining - 1, 0, deckVisualSprites.Length - 1);
                deckVisual.sprite = deckVisualSprites[level];
            }
        }

        if (hasCards && deckVisual != null)
            transform.position = GetSpawnPosition();

        SetVisible(hasCards);
        if (!hasCards && emptyStateVisual != null)
            emptyStateVisual.SetActive(true);
        RefreshCountText();
    }

    public void RestartDeck(IEnumerable<CardData> recycleFromPile)
    {
        var recycleSet = new HashSet<CardData>(recycleFromPile);
        var drawable = new List<CardData>();
        foreach (CardData c in originalCards)
            if (recycleSet.Contains(c))
                drawable.Add(c);
        cards = drawable.ToArray();
        drawIndex = 0;
        if (emptyStateVisual != null) emptyStateVisual.SetActive(false);
        UpdateDeckVisual();
        RefreshCountText();
    }

    public void SetCards(List<CardData> newCards)
    {
        cards = newCards.ToArray();
        originalCards = cards;
        drawIndex = 0;
        if (emptyStateVisual != null) emptyStateVisual.SetActive(false);
        UpdateDeckVisual();
        RefreshCountText();
    }

    public void SetCountDisplay(int count)
    {
        if (deckCountText != null)
            deckCountText.text = count.ToString();
    }

    public GameObject CreateBackgroundSpawn()
    {
        if (spawnContainer == null || RemainingCount <= 0) return null;

        GameObject bg = Instantiate(spawnContainer.gameObject, transform.parent);
        bg.transform.SetPositionAndRotation(spawnContainer.position, Quaternion.identity);

        foreach (SpriteRenderer sr in bg.GetComponentsInChildren<SpriteRenderer>(true))
            sr.sortingOrder = -5;
        foreach (Canvas c in bg.GetComponentsInChildren<Canvas>(true))
            c.sortingOrder = -5;
        foreach (TMP_Text t in bg.GetComponentsInChildren<TMP_Text>(true))
            t.text = RemainingCount.ToString();

        return bg;
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
            deck[i] = new CardData { cardName = names[i] };
        return deck;
    }
}
