using UnityEngine;

public class MoveCounter : MonoBehaviour
{
    public static event System.Action MovesChanged;
    public static event System.Action MovesExhausted;

    public static MoveCounter Instance { get; private set; }

    public int MovesRemaining { get; private set; }
    public int TotalMovesSpent { get; private set; }

    public static bool IsOutOfMoves => Instance != null && Instance.MovesRemaining <= 0;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        RevealPile.CardDrawnToRevealPile += OnMoveSpent;
        HandDropZone.CardTakenToHand += OnMoveSpent;
        ActiveCardSlot.CardPlayed += OnMoveSpent;
        HandManager.DeckRestarted += OnMoveSpent;
    }

    private void OnDisable()
    {
        RevealPile.CardDrawnToRevealPile -= OnMoveSpent;
        HandDropZone.CardTakenToHand -= OnMoveSpent;
        ActiveCardSlot.CardPlayed -= OnMoveSpent;
        HandManager.DeckRestarted -= OnMoveSpent;
    }

    public void Init(int maxMoves)
    {
        MovesRemaining = maxMoves;
        TotalMovesSpent = 0;
        MovesChanged?.Invoke();
    }

    public void AddMoves(int amount)
    {
        MovesRemaining += amount;
        MovesChanged?.Invoke();
    }

    private void OnMoveSpent()
    {
        MovesRemaining = Mathf.Max(0, MovesRemaining - 1);
        TotalMovesSpent++;
        MovesChanged?.Invoke();
        if (MovesRemaining == 0) MovesExhausted?.Invoke();
    }
}
