using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class LinearCardLayout
{
    private readonly Transform anchor;
    private readonly float spacing;

    public LinearCardLayout(Transform anchor, float spacing)
    {
        this.anchor = anchor;
        this.spacing = spacing;
    }

    public void PlaceCards(IReadOnlyList<Card> cards)
    {
        if (anchor == null)
        {
            Debug.LogError("[LinearCardLayout] anchor is null — wire the anchor field in the Inspector.");
            return;
        }
        int count = cards.Count;
        if (count == 0) return;

        float startX = -(count - 1) * spacing / 2f;

        for (int i = 0; i < count; i++)
        {
            if (cards[i].IsDragging) continue;

            Vector3 pos = anchor.position + anchor.right * (startX + i * spacing);

            cards[i].transform.DOMove(pos, 0.25f);
            cards[i].transform.DORotateQuaternion(anchor.rotation, 0.25f);
            cards[i].SetSortingOrder(i);
            cards[i].SetHorizontal(i == count - 1);
        }
    }
}
