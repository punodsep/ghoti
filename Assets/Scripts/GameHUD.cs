using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-10)]
public class GameHUD : MonoBehaviour
{
    [SerializeField] private FishDropper dropper;

    [Header("คะแนน / ปลาตัวถัดไป")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Image nextFishImage;

    [Header("สายวิวัฒนาการของ Deck")]
    [SerializeField] private Transform chainContainer;
    [SerializeField] private Image chainIconPrefab;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    private void Start()
    {
        GameManager gm = GameManager.Instance;

        gm.ScoreChanged += OnScoreChanged;
        gm.GameEnded += OnGameEnded;
        dropper.NextTierChanged += OnNextTierChanged;

        restartButton.onClick.AddListener(gm.Restart);
        menuButton.onClick.AddListener(gm.GoToMenu);
        gameOverPanel.SetActive(false);

        BuildChain();
        OnScoreChanged(gm.Score);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ScoreChanged -= OnScoreChanged;
            GameManager.Instance.GameEnded -= OnGameEnded;
        }
        if (dropper != null)
        {
            dropper.NextTierChanged -= OnNextTierChanged;
        }
    }

    private void BuildChain()
    {
        foreach (FishData data in GameManager.Instance.Deck)
        {
            Image icon = Instantiate(chainIconPrefab, chainContainer);
            icon.sprite = data.sprite;
            icon.preserveAspect = true;
        }
    }

    private void OnScoreChanged(int score)
    {
        scoreText.text = score.ToString("N0");
    }

    private void OnNextTierChanged(int tier)
    {
        nextFishImage.sprite = GameManager.Instance.Deck[tier].sprite;
        nextFishImage.preserveAspect = true;
    }

    private void OnGameEnded()
    {
        GameManager gm = GameManager.Instance;
        GameResult r = gm.Result;

        string text = $"Score: {gm.Score:N0}";
        if (r.newBest) text += "\nสถิติใหม่";
        text += $"\n+{r.coinsEarned} เหรียญ";
        foreach (FishData fish in r.newlyUnlocked)
        {
            text += $"\nปลดล็อก: {fish.displayName}";
        }

        finalScoreText.text = text;
        gameOverPanel.SetActive(true);
    }
}