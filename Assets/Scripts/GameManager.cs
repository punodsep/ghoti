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
    [SerializeField] private float minDiameter = 0.45f;
    [SerializeField] private float maxDiameter = 2.6f;

    [Header("กติกา")]
    [Tooltip("น้ำหนักสุ่มของแต่ละ layer (ช่อง 0 = layer 1) ก่อนผู้เล่นเคยมีปลา layer นั้นในโหลเกมนี้")]
    [SerializeField] private float[] dropWeightsLocked = { 30f, 30f, 25f, 15f, 1.5f, 0.5f, 0f };
    [Tooltip("น้ำหนักสุ่มหลังจากเคยมีปลา layer นั้นในโหลแล้ว (เกิดจากการรวมหรือสุ่มได้)")]
    [SerializeField] private float[] dropWeightsReached = { 30f, 30f, 25f, 15f, 5f, 3f, 0.5f };
    [Tooltip("การรวมที่เกิดจาก ability ได้สูงสุดกี่ครั้งต่อการปล่อย 1 ครั้ง (กันลูปไม่จบ)")]
    [SerializeField] private int maxChainMergesPerDrop = 20;
    [Tooltip("จำนวนปลาตัวถัดไปที่แสดงได้มากที่สุด (รวมตัวปกติ 1 ตัว)")]
    [SerializeField] private int maxUpcomingPreviews = 4;

    public event Action<int> ScoreChanged;
    public event Action GameEnded;
    public event Action<Vector2> TopMerged;               // ไว้ใส่เอฟเฟกต์/เสียงทีหลัง
    public event Action<MergeContext, int> Merged;        // ctx, คะแนนที่ได้จริง
    public event Action UpcomingChanged;
    public event Action<FishAbility, Vector2> AbilityTriggered;   // ไว้ใส่ VFX/SFX ของ ability

    private List<FishData> deck;
    private int mergeCount;
    private int topMergeCount;
    private int bonusCoins;
    private int chainMergesThisDrop;

    private readonly List<Fish> activeFish = new List<Fish>();
    private readonly List<int> upcoming = new List<int>();
    private readonly Dictionary<FishAbility, TimedMultiplier> timedMultipliers =
        new Dictionary<FishAbility, TimedMultiplier>();

    private struct TimedMultiplier
    {
        public float multiplier;
        public float endTime;
    }

    public GameResult Result { get; private set; }
    public IReadOnlyList<FishData> Deck => deck;
    public IReadOnlyList<Fish> ActiveFish => activeFish;
    public int Score { get; private set; }
    public bool IsGameOver { get; private set; }
    public int BonusCoins => bonusCoins;

    /// layer สูงสุด (index) ที่เคยมีในโหลเกมนี้ — ใช้ปลดน้ำหนักสุ่ม
    public int HighestTierReached { get; private set; } = -1;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;

        List<FishData> source = GameSession.HasValidDeck
            ? GameSession.SelectedDeck
            : DeckFromProfileOrDefault();

        deck = new List<FishData>(source);
        deck.Sort((x, y) => x.layer.CompareTo(y.layer));   // tier i = layer i+1 เสมอ

        if (deck.Count < GameSession.MinDeck || deck.Count > GameSession.MaxDeck)
        {
            Debug.LogError($"[GameManager] Deck ต้องมี {GameSession.MinDeck} ตัวพอดี (ตรวจ Default Deck ใน Inspector)");
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
        // โตเป็นอัตราส่วนเท่ากันทุกขั้น (แบบ Suika) ไม่ใช่บวกเท่ากันทุกขั้น
        return minDiameter * Mathf.Pow(maxDiameter / minDiameter, t);
    }

    public int RollTier()
    {
        // สุ่มแบบถ่วงน้ำหนัก: layer 1-4 ออกบ่อย, 5-6 แทบไม่ออกจนกว่าจะเคยมีในโหล, 7 ออกไม่ได้เลยจนกว่าจะเคยมี
        float total = 0f;
        for (int t = 0; t < deck.Count; t++) total += DropWeight(t);
        if (total <= 0f) return 0;

        float roll = UnityEngine.Random.value * total;
        for (int t = 0; t < deck.Count; t++)
        {
            roll -= DropWeight(t);
            if (roll < 0f) return t;
        }
        return 0;
    }

    public float DropWeight(int tier)
    {
        float[] w = tier <= HighestTierReached ? dropWeightsReached : dropWeightsLocked;
        return w != null && tier >= 0 && tier < w.Length ? Mathf.Max(0f, w[tier]) : 0f;
    }

    // ---------------------------------------------------------------- Upcoming queue

    private void FillUpcoming()
    {
        while (upcoming.Count < maxUpcomingPreviews)
        {
            upcoming.Add(RollTier());
        }
    }

    /// index 0 = ตัวถัดไปที่จะได้ถือ
    public int PeekUpcoming(int index)
    {
        FillUpcoming();
        return upcoming[Mathf.Clamp(index, 0, upcoming.Count - 1)];
    }

    public int TakeNextTier()
    {
        FillUpcoming();
        int tier = upcoming[0];
        upcoming.RemoveAt(0);
        FillUpcoming();
        UpcomingChanged?.Invoke();
        return tier;
    }

    public void SetUpcoming(int index, int tier)
    {
        FillUpcoming();
        if (index < 0 || index >= upcoming.Count) return;
        upcoming[index] = Mathf.Clamp(tier, 0, deck.Count - 1);
        UpcomingChanged?.Invoke();
    }

    /// จำนวนปลาตัวถัดไปที่ HUD ควรแสดง (1 + โบนัสจาก ability เช่น Angelfish)
    public int UpcomingPreviewCount
    {
        get
        {
            int extra = 0;
            foreach (Fish f in activeFish)
            {
                if (f != null && f.IsInPlay && f.Ability != null)
                {
                    extra += f.Ability.ExtraUpcomingPreviews;
                }
            }
            return Mathf.Clamp(1 + extra, 1, maxUpcomingPreviews);
        }
    }

    // ---------------------------------------------------------------- Fish lifecycle

    public Fish SpawnFish(int tier, Vector2 position, bool held)
    {
        Fish fish = Instantiate(fishPrefab, position, Quaternion.identity, fishParent);
        activeFish.Add(fish);
        fish.Init(deck[tier], tier, DiameterForTier(tier), held);
        if (tier > HighestTierReached) HighestTierReached = tier;

        if (!held)
        {
            fish.Release();
        }

        fish.PlayPop();
        return fish;
    }

    public void UnregisterFish(Fish fish)
    {
        activeFish.Remove(fish);
    }

    /// เรียกจาก FishDropper ทุกครั้งที่ผู้เล่นปล่อยปลา
    public void NotifyDropped()
    {
        chainMergesThisDrop = 0;
    }

    /// ลบปลาออกจากโหลทันที (ไม่ได้คะแนน) เช่น Swordfish Pierce
    public void RemoveFish(Fish fish)
    {
        if (fish == null || fish.IsMerged) return;
        fish.MarkMerged();
        fish.gameObject.SetActive(false);
        Destroy(fish.gameObject);
    }

    // ---------------------------------------------------------------- Merge

    public bool CanMerge(Fish a, Fish b)
    {
        if (a == null || b == null || a == b) return false;
        if (a.Tier == b.Tier) return true;

        return (a.Ability != null && a.Ability.CanMergeWith(a, b)) ||
               (b.Ability != null && b.Ability.CanMergeWith(b, a));
    }

    /// การรวมที่ ability สั่ง (เช่น Chain Zap) นับเป็นการรวมปกติ แต่ติดเพดานต่อการปล่อย 1 ครั้ง
    public bool TryAbilityMerge(Fish a, Fish b)
    {
        if (IsGameOver || a == null || b == null) return false;
        if (!a.IsInPlay || !b.IsInPlay) return false;
        if (chainMergesThisDrop >= maxChainMergesPerDrop) return false;
        if (!CanMerge(a, b)) return false;

        MergeFish(a, b, true);
        return true;
    }

    public void MergeFish(Fish a, Fish b, bool causedByAbility)
    {
        if (IsGameOver) return;

        a.MarkMerged();
        b.MarkMerged();
        if (causedByAbility) chainMergesThisDrop++;

        // รวมต่าง tier (Snakehead) → ได้ tier ถัดไปของตัวที่ใหญ่กว่า
        int highTier = Mathf.Max(a.Tier, b.Tier);
        int resultTier = highTier + 1;
        bool isTop = resultTier >= deck.Count;
        int n = highTier + 1;

        MergeContext ctx = new MergeContext
        {
            a = a,
            b = b,
            position = (a.transform.position + b.transform.position) * 0.5f,
            resultTier = resultTier,
            isTopMerge = isTop,
            causedByAbility = causedByAbility,
            baseScore = 5 * n * (n + 1),   // layer1=10, 2=30, 3=60, 4=100 ...
        };

        if (isTop) ctx.multiplier *= 2f;

        // effect ที่ทำงานอยู่ คำนวณก่อน hook เพื่อให้ Betta ไม่บูสต์การรวมที่เพิ่งเปิดมันเอง
        ctx.multiplier *= ActiveTimedMultiplier();

        // On Merge (score): ability ของคู่ที่รวม — ตัวละครั้ง (คู่ชนิดเดียวกัน = ครั้งเดียว)
        FishAbility abA = a.Ability;
        FishAbility abB = b.Ability;
        if (abA != null) abA.OnMergeScore(a, ctx, this);
        if (abB != null && abB != abA) abB.OnMergeScore(b, ctx, this);

        a.gameObject.SetActive(false);
        b.gameObject.SetActive(false);
        Destroy(a.gameObject);
        Destroy(b.gameObject);

        int gained = Mathf.RoundToInt(ctx.baseScore * ctx.multiplier);
        AddScore(gained);
        bonusCoins += ctx.bonusCoins;
        mergeCount++;
        Merged?.Invoke(ctx, gained);

        if (isTop)
        {
            topMergeCount++;
            TopMerged?.Invoke(ctx.position);
            return;
        }

        // ปลาใหม่เกิด → OnSpawned (ใน Init) → On Merge (created)
        Fish created = SpawnFish(resultTier, ctx.position, false);
        if (created.Ability != null)
        {
            created.Ability.OnMergeCreated(created, this);
        }
    }

    // ---------------------------------------------------------------- Timed effects

    /// เริ่ม/รีเฟรช ตัวคูณคะแนนชั่วคราว (เรียกซ้ำ = รีเฟรชเวลาเต็ม ไม่บวกเวลา)
    public void StartTimedMultiplier(FishAbility source, float multiplier, float duration)
    {
        timedMultipliers[source] = new TimedMultiplier
        {
            multiplier = multiplier,
            endTime = Time.time + duration,
        };
    }

    public float ActiveTimedMultiplier()
    {
        float m = 1f;
        foreach (TimedMultiplier t in timedMultipliers.Values)
        {
            if (Time.time < t.endTime) m *= t.multiplier;
        }
        return m;
    }

    /// เวลาที่เหลือของ effect (0 = ไม่ทำงาน) ไว้ให้ HUD แสดง
    public float TimedMultiplierRemaining(FishAbility source)
    {
        return timedMultipliers.TryGetValue(source, out TimedMultiplier t)
            ? Mathf.Max(0f, t.endTime - Time.time)
            : 0f;
    }

    public void RaiseAbilityTriggered(FishAbility ability, Vector2 position)
    {
        AbilityTriggered?.Invoke(ability, position);
    }

    // ---------------------------------------------------------------- Helpers for abilities

    /// ปลาที่อยู่ในโหล (ปล่อยแล้ว) ที่ขอบตัวอยู่ภายในรัศมี
    public List<Fish> FishInRadius(Vector2 center, float radius, Fish exclude = null)
    {
        List<Fish> result = new List<Fish>();
        foreach (Fish f in activeFish)
        {
            if (f == null || f == exclude || !f.IsInPlay) continue;
            float dist = Vector2.Distance(center, f.Body.position) - f.Radius;
            if (dist <= radius) result.Add(f);
        }
        return result;
    }

    // ---------------------------------------------------------------- Score & flow

    public void AddScore(int amount)
    {
        Score += amount;
        ScoreChanged?.Invoke(Score);
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Result = ProgressService.RecordGame(Score, mergeCount, topMergeCount, bonusCoins);
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
