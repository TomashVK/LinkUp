using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class LinearCardLayout
{
    private readonly Transform anchor;
    private readonly float spacing;
    private readonly float margin;
    private readonly bool centerOnSafeArea;

    public float ScrollOffset { get; set; }

    public LinearCardLayout(Transform anchor, float spacing, float margin, bool centerOnSafeArea = false)
    {
        this.anchor = anchor;
        this.spacing = spacing;
        this.margin = margin;
        this.centerOnSafeArea = centerOnSafeArea;
    }

    public void PlaceCards(IReadOnlyList<Card> cards, bool instant = false)
    {
        if (anchor == null)
        {
            Debug.LogError("[LinearCardLayout] anchor is null — wire the anchor field in the Inspector.");
            return;
        }
        int count = cards.Count;
        if (count == 0) return;

        Camera cam = Camera.main;
        float depth = cam.WorldToScreenPoint(anchor.position).z;
        Rect sv = SafeAreaHelper.GetViewportRect();
        float leftBound  = cam.ViewportToWorldPoint(new Vector3(sv.xMin, 0f, depth)).x + margin;
        float rightBound = cam.ViewportToWorldPoint(new Vector3(sv.xMax, 0f, depth)).x - margin;
        float available  = rightBound - leftBound;
        float centerX    = centerOnSafeArea ? (leftBound + rightBound) / 2f : anchor.position.x;
        float totalWidth = (count - 1) * spacing;

        // Right-anchor when overflow so newest cards are always visible at rest
        float rightEdge = totalWidth <= available ? centerX + totalWidth / 2f : rightBound;
        float baseX = rightEdge - totalWidth;

        // Clamp scroll: 0 = show newest (rightmost), max = fully reveal oldest (leftmost)
        float maxScroll = centerOnSafeArea ? Mathf.Max(0f, totalWidth - available) : 0f;
        float scroll = Mathf.Clamp(ScrollOffset, 0f, maxScroll);
        ScrollOffset = scroll; // write back so caller stays in sync — no dead-zone on reversal

        for (int i = 0; i < count; i++)
        {
            if (cards[i].IsDragging) continue;

            float naturalX = baseX + i * spacing + scroll;
            // Cards beyond either bound pile up at that edge
            float cardX = centerOnSafeArea ? Mathf.Clamp(naturalX, leftBound, rightBound) : naturalX;

            float xOffset = cardX - anchor.position.x;
            Vector3 pos = anchor.position + anchor.right * xOffset;

            cards[i].transform.DOKill();
            if (instant)
                cards[i].transform.SetPositionAndRotation(pos, anchor.rotation);
            else
            {
                cards[i].transform.DOMove(pos, 0.25f);
                cards[i].transform.DORotateQuaternion(anchor.rotation, 0.25f);
            }
            cards[i].SetSortingOrder(i);
            cards[i].SetHorizontal(i == count - 1);
        }
    }
}
