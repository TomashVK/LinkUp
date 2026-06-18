using DG.Tweening;
using UnityEngine;

public class ActiveCardSlot : MonoBehaviour, ICardDrop
{
    public static event System.Action CardPlayed;

    [SerializeField] private Vector2 dropOffset;

    private ConnectionGraph graph;
    private Card activeCard;

    public void Init(ConnectionGraph connectionGraph)
    {
        graph = connectionGraph;
    }

    public void ReceiveCard(Card card)
    {
        activeCard = card;
        RectTransform rt = card.GetComponent<RectTransform>();
        RectTransform slotRT = GetComponent<RectTransform>();
        rt.DOKill();
        rt.DOAnchorPos((Vector2)slotRT.localPosition + dropOffset, 0.25f);
        rt.DOLocalRotateQuaternion(slotRT.localRotation, 0.25f);
        card.SetHorizontal(true);
        card.SetSortingOrder(1);
        card.SetShadowSide(true);
    }

    public bool OnCardDrop(Card card)
    {
        if (activeCard == null) return false;
        if (graph == null) return false;
        if (!graph.CanPlay(activeCard.Data.gameId, card.Data.gameId)) return false;

        Destroy(activeCard.gameObject);
        activeCard = card;

        RectTransform rt = card.GetComponent<RectTransform>();
        RectTransform slotRT = GetComponent<RectTransform>();
        rt.DOKill();
        rt.DOAnchorPos((Vector2)slotRT.localPosition + dropOffset, 0.25f);
        rt.DOLocalRotateQuaternion(slotRT.localRotation, 0.25f);
        card.SetHorizontal(true);
        card.SetSortingOrder(1);
        card.SetShadowSide(true);

        CardPlayed?.Invoke();
        return true;
    }
}
