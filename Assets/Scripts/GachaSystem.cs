using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GachaResult
{
    public FishData fish;
    public bool isNew;
    public int refund;
}

public static class GachaSystem
{
    public const int PullCost = 100;
    public const int PityLimit = 30;     // ครบกี่ครั้งการันตี Epic ขึ้นไป

    private static int RarityWeight(FishRarity r)
    {
        switch (r)
        {
            case FishRarity.Common: return 60;
            case FishRarity.Rare: return 28;
            case FishRarity.Epic: return 10;
            default: return 2;
        }
    }

    private static int DuplicateRefund(FishRarity r)
    {
        switch (r)
        {
            case FishRarity.Common: return 30;
            case FishRarity.Rare: return 50;
            case FishRarity.Epic: return 80;
            default: return 150;
        }
    }

    public static List<FishData> GetPool()
    {
        return FishDatabase.Instance.allFish
            .Where(f => f != null && f.unlockType == UnlockType.Gacha)
            .ToList();
    }

    public static int PullsUntilPity =>
        Mathf.Max(0, PityLimit - SaveSystem.Profile.pityCounter);

    public static bool CanPull =>
        SaveSystem.Profile.coins >= PullCost && GetPool().Count > 0;

    public static bool TryPull(out GachaResult result)
    {
        result = null;
        PlayerProfile p = SaveSystem.Profile;
        List<FishData> pool = GetPool();

        if (pool.Count == 0 || p.coins < PullCost) return false;

        p.coins -= PullCost;
        p.gachaPulls++;
        p.pityCounter++;

        bool pity = p.pityCounter >= PityLimit;
        FishRarity rarity = RollRarity(pool, pity);

        List<FishData> candidates = pool.Where(f => f.rarity == rarity).ToList();
        FishData fish = candidates[Random.Range(0, candidates.Count)];

        if (rarity >= FishRarity.Epic) p.pityCounter = 0;

        bool isNew = p.Unlock(fish.id);
        int refund = isNew ? 0 : DuplicateRefund(rarity);
        p.coins += refund;

        SaveSystem.Save();

        result = new GachaResult { fish = fish, isNew = isNew, refund = refund };
        return true;
    }

    private static FishRarity RollRarity(List<FishData> pool, bool pity)
    {
        // ใช้เฉพาะระดับที่มีปลาอยู่ในพูลจริง
        List<FishRarity> available = pool.Select(f => f.rarity).Distinct().ToList();

        if (pity)
        {
            List<FishRarity> high = available.Where(r => r >= FishRarity.Epic).ToList();
            if (high.Count > 0) available = high;
        }

        int total = available.Sum(RarityWeight);
        int roll = Random.Range(0, total);

        foreach (FishRarity r in available)
        {
            roll -= RarityWeight(r);
            if (roll < 0) return r;
        }
        return available[0];
    }
}