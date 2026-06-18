using UnityEngine;

public class CardAnimationSettings : MonoBehaviour
{
    public static CardAnimationSettings Instance { get; private set; }

    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private float flipHalfDuration = 0.15f;
    [SerializeField] private float dealStagger = 0.12f;
    [Range(0f, 1f)]
    [SerializeField] private float flipStartPercent = 0.9f;

    public float MoveDuration => moveDuration;
    public float FlipHalfDuration => flipHalfDuration;
    public float DealStagger => dealStagger;
    public float FlipStartPercent => flipStartPercent;

    private void Awake()
    {
        Instance = this;
    }
}
