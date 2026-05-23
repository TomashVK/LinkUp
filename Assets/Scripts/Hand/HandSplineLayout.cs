using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Splines;

public sealed class HandSplineLayout
{
    private readonly SplineContainer _splineContainer;
    private readonly float _spacing;

    public float ScrollOffset { get; set; }

    public HandSplineLayout(SplineContainer splineContainer, float spacing)
    {
        _splineContainer = splineContainer;
        _spacing = spacing;
    }

    public bool IsScrollable(int cardCount) =>
        cardCount > 1 && (cardCount - 1) * _spacing > 1f;

    public float ClampOffset(float offset, int cardCount)
    {
        float halfSpan  = (cardCount - 1) * _spacing / 2f;
        float minOffset = halfSpan - 0.5f;
        float maxOffset = 0.5f - halfSpan;
        return minOffset <= maxOffset
            ? Mathf.Clamp(offset, minOffset, maxOffset)
            : Mathf.Clamp(offset, maxOffset, minOffset);
    }

    public float ScreenDeltaToSplineDelta(float screenDeltaX)
    {
        Camera cam = Camera.main;
        if (cam == null) return 0f;
        Spline spline = _splineContainer.Spline;
        const float dt = 0.01f;
        float refT = Mathf.Clamp(0.5f + ScrollOffset, 0f, 1f - dt);
        float screenDist = cam.WorldToScreenPoint(spline.EvaluatePosition(refT + dt)).x
                         - cam.WorldToScreenPoint(spline.EvaluatePosition(refT)).x;
        return Mathf.Abs(screenDist) < 0.001f ? 0f : screenDeltaX * dt / screenDist;
    }

    public void PlaceCards(IReadOnlyList<Card> cards, bool isDragging)
    {
        int cardCount = cards.Count;
        if (cardCount == 0) return;

        if (!IsScrollable(cardCount)) ScrollOffset = 0f;
        ScrollOffset = ClampOffset(ScrollOffset, cardCount);

        float firstT = 0.5f + ScrollOffset - (cardCount - 1) * _spacing / 2f;
        Spline spline = _splineContainer.Spline;

        for (int i = 0; i < cardCount; i++)
        {
            cards[i].SetHorizontal(i == cardCount - 1);
            if (cards[i].IsDragging) continue;

            float t = firstT + i * _spacing;
            Vector3 pos     = spline.EvaluatePosition(t);
            Vector3 forward = spline.EvaluateTangent(t);
            Vector3 up      = spline.EvaluateUpVector(t);
            Quaternion rot  = Quaternion.LookRotation(up, Vector3.Cross(up, forward).normalized);

            if (isDragging)
            {
                DOTween.Kill(cards[i].transform);
                cards[i].transform.SetPositionAndRotation(pos, rot);
            }
            else
            {
                cards[i].transform.DOMove(pos, 0.25f);
                cards[i].transform.DORotateQuaternion(rot, 0.25f);
            }

            cards[i].SetSortingOrder(i);
        }
    }
}
