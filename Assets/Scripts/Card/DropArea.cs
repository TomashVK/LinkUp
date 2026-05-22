using System.Collections.Generic;
using UnityEngine;

public class DropArea : MonoBehaviour, ICardDrop
{
    private readonly List<Card> stack = new();

    public void OnCardDrop(Card card)
    {
        stack.Add(card);
        card.transform.position = transform.position;
        card.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-5f, 5f));
        card.SetSortingOrder(stack.Count - 1);
        card.SetHorizontal(true);
    }
}
