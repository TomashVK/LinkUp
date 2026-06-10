using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardDeck : MonoBehaviour, IPointerClickHandler
{
    public static event System.Action Tapped;

    [SerializeField] private CardData[] cards;
    [SerializeField] private Image deckVisual;
    [SerializeField] private Sprite[] deckVisualSprites;
    [SerializeField] private TMP_Text deckCountText;
    [SerializeField] private Transform spawnContainer;

    private int drawIndex;
    private CardData[] originalCards;

    public bool HasCards => drawIndex < cards.Length;
    public int RemainingCount => cards.Length - drawIndex;

    private void Awake()
    {
        if (cards == null || cards.Length == 0)
            cards = CreateFakeDeck();
        originalCards = cards;
        RefreshCountText();
    }

    public void OnPointerClick(PointerEventData eventData) => Tapped?.Invoke();

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

    public void UpdateDeckSprite()
    {
        if (deckVisual == null || deckVisualSprites == null || deckVisualSprites.Length == 0) return;
        int remaining = RemainingCount;
        if (remaining > 0)
        {
            int level = Mathf.Clamp(remaining - 1, 0, deckVisualSprites.Length - 1);
            deckVisual.sprite = deckVisualSprites[level];
        }
    }

    public Vector2 GetSpawnPosition()
    {
        return transform.localPosition;
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

        if (deckCountText != null)
            deckCountText.gameObject.SetActive(hasCards);

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
        UpdateDeckVisual();
        RefreshCountText();
    }

    public void SetCards(List<CardData> newCards)
    {
        cards = newCards.ToArray();
        originalCards = cards;
        drawIndex = 0;
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
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        RectTransform scRT = spawnContainer.GetComponent<RectTransform>();
        if (bgRT != null && scRT != null)
        {
            bgRT.position = scRT.position;
            bg.transform.SetSiblingIndex(transform.GetSiblingIndex());
        }

        if (deckCountText != null)
        {
            GameObject textClone = Instantiate(deckCountText.gameObject, bg.transform);
            textClone.GetComponent<RectTransform>().position = deckCountText.GetComponent<RectTransform>().position;
            TMP_Text t = textClone.GetComponent<TMP_Text>();
            if (t != null) t.text = RemainingCount.ToString();
        }

        foreach (Canvas c in bg.GetComponentsInChildren<Canvas>(true))
            c.sortingOrder = -5;

        return bg;
    }

    private void RefreshCountText()
    {
        if (deckCountText != null)
            deckCountText.text = RemainingCount.ToString();
    }

    public void SetVisible(bool visible)
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = visible ? 1f : 0f;
            cg.blocksRaycasts = visible;
        }
        else
        {
            foreach (Graphic g in GetComponentsInChildren<Graphic>(true))
                g.enabled = visible;
            foreach (Canvas c in GetComponentsInChildren<Canvas>(true))
                c.enabled = visible;
        }
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
