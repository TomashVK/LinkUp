using System.Collections.Generic;
using UnityEngine;

public class DropArea : MonoBehaviour, ICardDrop
{
    private readonly List<Card> stack = new();

    public void OnCardDrop(Card card)
    {
        if (!card.IsHorizontal)
            card = SwapToHorizontal(card);

        stack.Add(card);
        card.transform.position = transform.position;
        card.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-5f, 5f));
        card.SetSortingOrder(stack.Count - 1);
        card.SetHorizontal(true);
    }

    private Card SwapToHorizontal(Card oldCard)
    {
        GameObject prefab = HandManager.HorizontalCardPrefab;
        if (prefab == null) return oldCard;

        CardData data = oldCard.Data;
        oldCard.transform.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);
        DG.Tweening.DOTween.Kill(oldCard.transform);
        Destroy(oldCard.gameObject);

        Card newCard = Instantiate(prefab, pos, rot).GetComponent<Card>();
        newCard.Init(data);
        return newCard;
    }
}
