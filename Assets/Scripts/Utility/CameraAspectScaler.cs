using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CameraAspectScaler : MonoBehaviour
{
    private Camera mainCamera;

    [Header("Figma Reference Dimensions")]
    public float figmaWidth = 1170f;
    public float figmaHeight = 2532f;
    public float pixelsPerUnit = 100f;

    void Awake()
    {
        mainCamera = GetComponent<Camera>();
        mainCamera.orthographic = true;
        UpdateCameraSize();
    }

    void Update()
    {
        #if UNITY_EDITOR
        UpdateCameraSize();
        #endif
    }

    void UpdateCameraSize()
    {
        if (mainCamera == null) return;

        float targetWidthUnits = figmaWidth / pixelsPerUnit;
        float targetHeightUnits = figmaHeight / pixelsPerUnit;

        float targetAspect = figmaWidth / figmaHeight;
        float currentDeviceAspect = (float)Screen.width / Screen.height;

        float idealHalfHeight = targetHeightUnits / 2f;

        if (currentDeviceAspect < targetAspect)
        {
            mainCamera.orthographicSize = idealHalfHeight * (targetAspect / currentDeviceAspect);
        }
        else
        {
            mainCamera.orthographicSize = idealHalfHeight;
        }
    }
}