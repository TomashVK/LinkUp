using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class PointerInputService : MonoBehaviour
{
    public static PointerInputService Instance { get; private set; }

    public event Action<Vector2> Pressed;
    public event Action<Vector2> Released;
    public event Action<Vector2> Moved;

    public Vector2 Position => positionAction.ReadValue<Vector2>();
    public bool IsCardDragging { get; set; }

    private InputAction pressAction;
    private InputAction positionAction;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        pressAction = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
        positionAction = new InputAction("Position", InputActionType.Value, "<Pointer>/position", expectedControlType: "Vector2");
    }

    private void OnEnable()
    {
        pressAction.started += OnPressStarted;
        pressAction.canceled += OnPressCanceled;
        positionAction.performed += OnPositionPerformed;
        pressAction.Enable();
        positionAction.Enable();
    }

    private void OnDisable()
    {
        pressAction.started -= OnPressStarted;
        pressAction.canceled -= OnPressCanceled;
        positionAction.performed -= OnPositionPerformed;
        pressAction.Disable();
        positionAction.Disable();
    }

    private void OnPressStarted(InputAction.CallbackContext ctx)
    {
        Vector2 pos = ctx.control?.device is Pointer ptr ? ptr.position.ReadValue() : positionAction.ReadValue<Vector2>();
        Pressed?.Invoke(pos);
    }

    private void OnPressCanceled(InputAction.CallbackContext ctx)
    {
        Vector2 pos = ctx.control?.device is Pointer ptr ? ptr.position.ReadValue() : positionAction.ReadValue<Vector2>();
        Released?.Invoke(pos);
    }

    private void OnPositionPerformed(InputAction.CallbackContext ctx)
    {
        Moved?.Invoke(ctx.ReadValue<Vector2>());
    }
}
