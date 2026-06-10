using UnityEngine;

[ExecuteInEditMode]
public class SafeAreaPadding : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea = new Rect(0, 0, 0, 0);

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeAreaPadding();
    }

    void Update()
    {
        if (Screen.safeArea != lastSafeArea)
            ApplySafeAreaPadding();
    }

    void ApplySafeAreaPadding()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) return;

        lastSafeArea = Screen.safeArea;
        Rect sa = Screen.safeArea;

        Vector2 anchorMin = new Vector2(sa.x / Screen.width, sa.y / Screen.height);
        Vector2 anchorMax = new Vector2((sa.x + sa.width) / Screen.width, (sa.y + sa.height) / Screen.height);

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
