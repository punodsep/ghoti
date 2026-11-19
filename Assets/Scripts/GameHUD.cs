using System.Collections.Generic;
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

    [Header("ปลาตัวถัดไปเพิ่มเติม (Angelfish) — สร้างจาก NextFishImage อัตโนมัติ")]
    [SerializeField] private float extraPreviewScale = 0.65f;
    [SerializeField] private float extraPreviewSpacing = 6f;

    [Header("ตัวคูณคะแนนชั่วคราว (Betta) — ว่างได้")]
    [SerializeField] private TMP_Text multiplierText;

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
        gm.UpcomingChanged += RefreshUpcoming;

        restartButton.onClick.AddListener(gm.Restart);
        menuButton.onClick.AddListener(gm.GoToMenu);
        gameOverPanel.SetActive(false);

        BuildChain();
        OnScoreChanged(gm.Score);
        RefreshUpcoming();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ScoreChanged -= OnScoreChanged;
            GameManager.Instance.GameEnded -= OnGameEnded;
            GameManager.Instance.UpcomingChanged -= RefreshUpcoming;
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

    private readonly List<Image> extraPreviews = new List<Image>();
    private int shownPreviewCount = -1;

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.UpcomingPreviewCount != shownPreviewCount)
        {
            RefreshUpcoming();
        }

        if (multiplierText != null)
        {
            float m = gm.ActiveTimedMultiplier();
            bool active = m > 1.001f && !gm.IsGameOver;
            multiplierText.gameObject.SetActive(active);
            if (active) multiplierText.text = $"x{m:0.##}";
        }
    }

    private void RefreshUpcoming()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || nextFishImage == null) return;

        nextFishImage.sprite = gm.Deck[gm.PeekUpcoming(0)].sprite;
        nextFishImage.preserveAspect = true;

        shownPreviewCount = gm.UpcomingPreviewCount;
        int extra = shownPreviewCount - 1;

        while (extraPreviews.Count < extra)
        {
            extraPreviews.Add(CreateExtraPreview(extraPreviews.Count));
        }

        for (int i = 0; i < extraPreviews.Count; i++)
        {
            bool show = i < extra;
            extraPreviews[i].gameObject.SetActive(show);
            if (show)
            {
                extraPreviews[i].sprite = gm.Deck[gm.PeekUpcoming(i + 1)].sprite;
            }
        }
    }

    /// สร้างรูปเล็กเรียงลงใต้ NextFishImage (ใช้ anchor/pivot เดียวกับต้นฉบับ)
    private Image CreateExtraPreview(int index)
    {
        RectTransform src = nextFishImage.rectTransform;
        Image img = Instantiate(nextFishImage, src.parent);
        img.name = $"NextFishImage_Extra{index + 1}";
        img.preserveAspect = true;
        img.raycastTarget = false;

        RectTransform rt = img.rectTransform;
        Vector2 pivot = src.pivot;
        Vector2 size = src.sizeDelta;
        Vector2 small = size * extraPreviewScale;
        rt.sizeDelta = small;

        // ต่อลงไปด้านล่างของรูปหลัก จัดกึ่งกลางแนวนอนให้ตรงกัน
        float srcBottom = src.anchoredPosition.y - size.y * pivot.y;
        float top = srcBottom - extraPreviewSpacing - index * (small.y + extraPreviewSpacing);
        float y = top - small.y * (1f - pivot.y);
        float centerX = src.anchoredPosition.x + size.x * (0.5f - pivot.x);
        float x = centerX - small.x * (0.5f - pivot.x);
        rt.anchoredPosition = new Vector2(x, y);
        return img;
    }

    private void OnGameEnded()
    {
        GameManager gm = GameManager.Instance;
        GameResult r = gm.Result;

        string text = $"Score: {gm.Score:N0}";
        if (r.newBest) text += "\nสถิติใหม่";
        text += $"\n+{r.coinsEarned} เหรียญ";
        if (gm.BonusCoins > 0) text += $" (Golden Scales +{gm.BonusCoins})";
        foreach (FishData fish in r.newlyUnlocked)
        {
            text += $"\nปลดล็อก: {fish.displayName}";
        }

        finalScoreText.text = text;
        gameOverPanel.SetActive(true);
    }
}