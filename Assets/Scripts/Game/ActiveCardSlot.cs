using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ActiveCardSlot : MonoBehaviour, ICardDrop
{
    public static event System.Action CardPlayed;

    private ConnectionGraph graph;
    private string activeCardId;
    private Card currentCard;

    public string ActiveCardId => activeCardId;

    public void Init(ConnectionGraph graph) => this.graph = graph;

    public void ReceiveCard(Card card)
    {
        PlaceCard(card);
    }

    public bool OnCardDrop(Card card)
    {
        if (MoveCounter.IsOutOfMoves) return false;

        if (graph == null)
        {
            Debug.LogWarning("[ActiveCardSlot.OnCardDrop] graph is null — rejected.");
            return false;
        }
        if (string.IsNullOrEmpty(card.Data?.gameId))
        {
            Debug.LogWarning("[ActiveCardSlot.OnCardDrop] card has no gameId — rejected.");
            return false;
        }

        bool canPlay = graph.CanPlay(activeCardId, card.Data.gameId);
        Debug.Log($"[ActiveCardSlot.OnCardDrop] CanPlay(active='{activeCardId}', drop='{card.Data.gameId}') = {canPlay}");

        if (!canPlay) return false;

        PlaceCard(card);
        CardPlayed?.Invoke();
        return true;
    }

    private void PlaceCard(Card card)
    {
        if (currentCard != null)
            Destroy(currentCard.gameObject);

        activeCardId = card.Data.gameId;
        card.transform.position = transform.position;
        card.SetSortingOrder(0);
        card.SetInteractable(false);
        currentCard = card;
        Debug.Log($"[ActiveCardSlot.PlaceCard] activeCardId='{activeCardId}' placed at {transform.position}.");
    }
}
