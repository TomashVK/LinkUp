using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    private static int currentLevelId = 1;

    [SerializeField] private HandManager handManager;
    [SerializeField] private CardDeck cardDeck;
    [SerializeField] private ActiveCardSlot activeCardSlot;
    [SerializeField] private MoveCounter moveCounter;
    [SerializeField] private ContinuePanel continuePanel;
    [SerializeField] private TMP_Text moveCountText;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private HudStarDisplay hudStarDisplay;

    private ConnectionGraph graph;
    private LevelDefinition level;

    private void Start()
    {
        LevelLoader.LoadAll();
        graph = new ConnectionGraph();
        graph.Build(LevelLoader.Cards, LevelLoader.TagRules, LevelLoader.Connections);
        LoadLevel(currentLevelId);
    }

    private void OnEnable()
    {
        HandManager.CardLeftHand += CheckWin;
        ActiveCardSlot.CardPlayed += CheckWin;
        MoveCounter.MovesChanged += RefreshMoveHUD;
        MoveCounter.MovesExhausted += OnMovesExhausted;
    }

    private void OnDisable()
    {
        HandManager.CardLeftHand -= CheckWin;
        ActiveCardSlot.CardPlayed -= CheckWin;
        MoveCounter.MovesChanged -= RefreshMoveHUD;
        MoveCounter.MovesExhausted -= OnMovesExhausted;
    }

    private void LoadLevel(int id)
    {
        level = LevelLoader.GetLevel(id);
        if (level == null)
        {
            Debug.LogError($"GameController: level {id} not found.");
            return;
        }

        moveCounter.Init(level.maxMoves);
        if (hudStarDisplay != null) hudStarDisplay.Init(level.maxMoves, level.optimalMoves);
        RefreshMoveHUD();

        if (winPanel != null) winPanel.SetActive(false);
        if (continuePanel != null) continuePanel.gameObject.SetActive(false);

        var allCards = new[] { MakeCardData(level.activeCard) }
            .Concat(level.hand.Select(MakeCardData))
            .Concat(level.deck.Select(MakeCardData))
            .ToList();

        cardDeck.SetCards(allCards);
        activeCardSlot.Init(graph);
        StartCoroutine(handManager.DealInitial(level.hand.Length, activeCardSlot));
    }

    private static CardData MakeCardData(string cardId)
    {
        CardDefinition def = LevelLoader.GetCard(cardId);
        return new CardData { cardName = def.text, gameId = def.id };
    }

    private void CheckWin()
    {
        if (handManager.CardCount == 0) ShowWin();
    }

    private void ShowWin()
    {
        if (winPanel != null) winPanel.SetActive(true);
    }

    private void OnMovesExhausted()
    {
        if (continuePanel != null) continuePanel.Show();
    }

    private void RefreshMoveHUD()
    {
        if (moveCountText != null && moveCounter != null)
            moveCountText.text = $"{moveCounter.MovesRemaining}";
    }

    public void RestartLevel() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public void LoadNextLevel()
    {
        currentLevelId++;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
