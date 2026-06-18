using UnityEngine.EventSystems;
using UnityEngine;

public class RevertButton : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        UndoManager.Instance?.PerformUndo();
    }
}
