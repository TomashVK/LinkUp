using UnityEngine;

[ExecuteInEditMode]
public class SafeAreaPadding : MonoBehaviour
{
    private Camera mainCamera;
    private Rect lastSafeArea = new Rect(0, 0, 0, 0);

    void Start()
    {
        mainCamera = Camera.main;
        ApplySafeAreaPadding();
    }

    void Update()
    {
        if (Screen.safeArea != lastSafeArea)
        {
            ApplySafeAreaPadding();
        }
    }

    void ApplySafeAreaPadding()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        lastSafeArea = Screen.safeArea;

        // 1. Get the hardware safe area boundaries
        Rect safeArea = Screen.safeArea;

        // 2. Convert pixel offsets into Unity World Units
        Vector3 bottomLeftWorld = mainCamera.ScreenToWorldPoint(new Vector3(safeArea.x, safeArea.y, 0));
        Vector3 topRightWorld = mainCamera.ScreenToWorldPoint(new Vector3(safeArea.x + safeArea.width, safeArea.y + safeArea.height, 0));
        Vector3 absoluteTopWorld = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));

        // 3. Calculate how many world units the notch takes up at the top
        float notchHeightUnits = absoluteTopWorld.y - topRightWorld.y;

        // 4. Shift this container down exactly by the notch height padding
        if (notchHeightUnits > 0)
        {
            transform.localPosition = new Vector3(0, -notchHeightUnits, transform.localPosition.z);
        }
        else
        {
            transform.localPosition = new Vector3(0, 0, transform.localPosition.z);
        }
    }
}