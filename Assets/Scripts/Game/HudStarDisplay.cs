using UnityEngine;

public class HudStarDisplay : MonoBehaviour
{
    [SerializeField] StarDisplay[] stars; // index 0 = left (drains last), 2 = right (drains first)

    int optimal;
    int perStar;   // extra-move budget for the first two stars
    int lastZone;  // budget for the final (leftmost) star

    public void Init(int maxMoves, int optimalMoves)
    {
        optimal  = optimalMoves;
        int buf  = Mathf.Max(3, maxMoves - optimalMoves);
        perStar  = Mathf.CeilToInt(buf / 3f);
        lastZone = Mathf.Max(1, buf - perStar * 2);
        Refresh(0);
    }

    void OnEnable()  => MoveCounter.MovesChanged += OnMovesChanged;
    void OnDisable() => MoveCounter.MovesChanged -= OnMovesChanged;

    void OnMovesChanged() => Refresh(MoveCounter.Instance.TotalMovesSpent);

    void Refresh(int spent)
    {
        int extra = Mathf.Max(0, spent - optimal);
        stars[2].SetState(StarState(extra, 0,           perStar));
        stars[1].SetState(StarState(extra, perStar,     perStar));
        stars[0].SetState(StarState(extra, perStar * 2, lastZone));
    }

    // Returns state 0–5 for a star whose drain zone starts at zoneStart with zoneBudget moves.
    // Guarantees state 5 is returned when extra == zoneStart (zone just entered),
    // so state 5 is never skipped even when moves jump multiple sub-states at once.
    static int StarState(int extra, int zoneStart, int zoneBudget)
    {
        int local = extra - zoneStart;
        if (local <= 0)          return 5;
        if (local >= zoneBudget) return 0;
        return Mathf.Max(0, 5 - local * 6 / zoneBudget);
    }

    public int StarsEarned =>
        (stars[0].CurrentState > 0 ? 1 : 0) +
        (stars[1].CurrentState > 0 ? 1 : 0) +
        (stars[2].CurrentState > 0 ? 1 : 0);
}
