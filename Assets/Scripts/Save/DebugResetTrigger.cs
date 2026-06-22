using UnityEngine;
using UnityEngine.EventSystems;

public class DebugResetTrigger : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private int requiredTaps = 7;
    [SerializeField] private float tapWindowSeconds = 2f;

    private int tapCount;
    private float windowStart;

    public void OnPointerClick(PointerEventData eventData)
    {
        float now = Time.unscaledTime;
        if (tapCount == 0 || now - windowStart > tapWindowSeconds)
        {
            tapCount = 0;
            windowStart = now;
        }

        tapCount++;
        if (tapCount < requiredTaps) return;

        tapCount = 0;
        Debug.Log("[DebugResetTrigger] Reset tap sequence reached — wiping save data.");
        SaveService.Instance.ResetAll();
    }
}
