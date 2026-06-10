using UnityEngine;
using UnityEngine.EventSystems;

public class HandSwipeArea : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
{
    public static event System.Action<float> Scrolled;

    public void OnPointerDown(PointerEventData eventData) { }
    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (HandManager.IsAnimating) return;
        if (Mathf.Abs(eventData.delta.x) > 0.0001f)
            Scrolled?.Invoke(eventData.delta.x);
    }
}
