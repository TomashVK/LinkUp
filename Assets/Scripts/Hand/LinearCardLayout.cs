using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class LinearCardLayout
{
    private readonly Transform anchor;
    private readonly float spacing;
    private readonly float margin;
    private readonly bool centerOnSafeArea;
    private readonly bool rightAnchored;

    public float ScrollOffset { get; set; }
    public bool Mirrored { get; set; }
    public bool UseVerticalRight { get; set; }

    public LinearCardLayout(Transform anchor, float spacing, float margin, bool centerOnSafeArea = false, bool rightAnchored = false)
    {
        this.anchor = anchor;
        this.spacing = spacing;
        this.margin = margin;
        this.centerOnSafeArea = centerOnSafeArea;
        this.rightAnchored = rightAnchored;
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

        float rightEdge = rightAnchored ? anchor.position.x : (totalWidth <= available ? centerX + totalWidth / 2f : rightBound);
        float baseX = rightEdge - totalWidth;

        float maxScroll = centerOnSafeArea ? Mathf.Max(0f, totalWidth - available) : 0f;
        float scroll = Mathf.Clamp(ScrollOffset, 0f, maxScroll);
        ScrollOffset = scroll;

        for (int i = 0; i < count; i++)
        {
            if (cards[i].IsDragging) continue;

            int posIdx = Mirrored ? (count - 1 - i) : i;
            float naturalX = baseX + posIdx * spacing + scroll;
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
            bool isTop = i == count - 1;
            cards[i].SetHorizontal(isTop);
            if (!isTop) cards[i].SetVerticalRight(UseVerticalRight);
        }
    }
}
