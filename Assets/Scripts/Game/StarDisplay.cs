using UnityEngine;
using UnityEngine.UI;

public class StarDisplay : MonoBehaviour
{
    [SerializeField] Sprite[] stateSprites;

    Image image;

    public int CurrentState { get; private set; }

    void Awake() => image = GetComponent<Image>();

    public void SetState(int state)
    {
        CurrentState = Mathf.Clamp(state, 0, stateSprites.Length - 1);
        image.sprite = stateSprites[CurrentState];
    }
}
