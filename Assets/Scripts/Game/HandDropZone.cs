using UnityEngine;

public class HandDropZone : MonoBehaviour, ICardDrop
{
    public static event System.Action CardTakenToHand;

    [SerializeField] private RevealPile revealPile;
    [SerializeField] private HandManager handManager;

    public bool OnCardDrop(Card card)
    {
        if (!revealPile.IsCardInPile(card)) return false;
        if (!handManager.CanAcceptCard()) return false;
        handManager.AddCardFromRevealPile(card);
        CardTakenToHand?.Invoke();
        return true;
    }
}
