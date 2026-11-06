using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Prefab & Parent")]
    [SerializeField] private Fish fishPrefab;
    [SerializeField] private Transform fishParent;

    [Header("Deck สำหรับเทสต์ (ใช้เมื่อยังไม่มีหน้าจัด Deck)")]
    [SerializeField] private List<FishData> defaultDeck = new List<FishData>();

    [Header("ขนาดปลา (เส้นผ่านศูนย์กลาง)")]
    [SerializeField] private float minDiameter = 0.7f;
    [SerializeField] private float maxDiameter = 2.6f;

    [Header("กติกา")]
    [SerializeField] private int maxDroppableTiers = 5;

    public event Action<int> ScoreChanged;
    public event Action GameEnded;
    public event Action<Vector2> TopMerged;   // ไว้ใส่เอฟเฟกต์/เสียงทีหลัง

    private List<FishData> deck;
    private int mergeCount;
    private int topMergeCount;
    public GameResult Result { get; private set; }

    public IReadOnlyList<FishData> Deck => deck;
    public int Score { get; private set; }
    public bool IsGameOver { get; private set; }

    public int DroppableTierCount =>
        Mathf.Clamp(deck.Count - 1, 1, maxDroppableTiers);

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;

        List<FishData> source = GameSession.HasValidDeck
            ? GameSession.SelectedDeck
            : DeckFromProfileOrDefault();

        deck = new List<FishData>(source);

        if (deck.Count < GameSession.MinDeck || deck.Count > GameSession.MaxDeck)
        {
            Debug.LogError("[GameManager] Deck ต้องมี 1-7 ตัว (ตรวจ Default Deck ใน Inspector)");
        }
    }

    private List<FishData> DeckFromProfileOrDefault()
    {
        if (FishDatabase.Instance != null)
        {
            List<FishData> saved = SaveSystem.Profile.GetDeck();
            if (saved.Count >= GameSession.MinDeck && saved.Count <= GameSession.MaxDeck)
            {
                return saved;
            }
        }
        return defaultDeck;
    }

    public float DiameterForTier(int tier)
    {
        float t = deck.Count <= 1 ? 0f : (float)tier / (deck.Count - 1);
        return Mathf.Lerp(minDiameter, maxDiameter, t);
    }

    public int RollTier()
    {
        return UnityEngine.Random.Range(0, DroppableTierCount);
    }

    public Fish SpawnFish(int tier, Vector2 position, bool held)
    {
        Fish fish = Instantiate(fishPrefab, position, Quaternion.identity, fishParent);
        fish.Init(deck[tier], tier, DiameterForTier(tier), held);

        if (!held)
        {
            fish.Release();
        }

        fish.PlayPop();
        return fish;
    }

    public void MergeFish(Fish a, Fish b)
    {
        if (IsGameOver) return;

        int tier = a.Tier;
        Vector2 mid = (a.transform.position + b.transform.position) * 0.5f;
        FishAbility mergeAbility = a.Data.ability;

        a.gameObject.SetActive(false);
        b.gameObject.SetActive(false);
        Destroy(a.gameObject);
        Destroy(b.gameObject);

        bool isTop = tier + 1 >= deck.Count;

        int baseScore = (tier + 1) * (tier + 2) * 5;   // tier0=10, 1=30, 2=60, 3=100 ...
        if (isTop) baseScore *= 2;

        int gained = mergeAbility != null
            ? mergeAbility.ModifyMergeScore(baseScore)
            : baseScore;

        AddScore(gained);
        mergeCount++;

        if (isTop)
        {
            topMergeCount++;
            TopMerged?.Invoke(mid);
            return;
        }

        Fish created = SpawnFish(tier + 1, mid, false);
        if (created.Data.ability != null)
        {
            created.Data.ability.OnMerged(created, this);
        }
    }

    public void AddScore(int amount)
    {
        Score += amount;
        ScoreChanged?.Invoke(Score);
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Result = ProgressService.RecordGame(Score, mergeCount, topMergeCount);
        GameEnded?.Invoke();
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}