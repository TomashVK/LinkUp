using UnityEngine;

public class WildCardButton : MonoBehaviour
{
    [SerializeField] private GameObject wildCardPrefab;
    [SerializeField] private HandManager handManager;
    [SerializeField] private RectTransform spawnPoint;
    [SerializeField] private ConsumableButton consumableButton;

    private void Awake()
    {
        if (consumableButton != null)
            consumableButton.CanActivate = () => handManager.CanAcceptCard();
    }

    public void SpawnWildCard()
    {
        if (!handManager.CanAcceptCard()) return;

        RectTransform container = handManager.CardContainer != null
            ? handManager.CardContainer
            : handManager.GetComponent<RectTransform>().root.GetComponent<RectTransform>();

        GameObject newCardObj = Instantiate(wildCardPrefab, container);
        RectTransform newCardRT = newCardObj.GetComponent<RectTransform>();
        newCardRT.anchoredPosition = container.InverseTransformPoint(spawnPoint.position);

        Card card = newCardObj.GetComponent<Card>();
        card.Init(new CardData { cardName = "", isWild = true });
        card.SetHorizontal(true);

        handManager.AddCardFromRevealPile(card);
    }
}
