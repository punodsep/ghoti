using System.Collections.Generic;
using UnityEngine;

public class GameResult
{
    public int coinsEarned;
    public bool newBest;
    public List<FishData> newlyUnlocked = new List<FishData>();
}

public static class ProgressService
{
    private const int ScorePerCoin = 30;

    /// เรียกครั้งเดียวตอนจบเกม: อัปเดตสถิติ, แจกเหรียญ, ตรวจเควสต์, เซฟ
    public static GameResult RecordGame(int score, int merges, int topMerges)
    {
        PlayerProfile p = SaveSystem.Profile;
        GameResult result = new GameResult();

        p.gamesPlayed++;
        p.totalMerges += merges;
        p.topMerges += topMerges;

        if (score > p.bestScore)
        {
            p.bestScore = score;
            result.newBest = true;
        }

        result.coinsEarned = Mathf.Max(1, score / ScorePerCoin);
        p.coins += result.coinsEarned;

        result.newlyUnlocked = CheckUnlocks();

        SaveSystem.Save();
        return result;
    }

    public static List<FishData> CheckUnlocks()
    {
        PlayerProfile p = SaveSystem.Profile;
        List<FishData> unlocked = new List<FishData>();

        foreach (FishData fish in FishDatabase.Instance.allFish)
        {
            if (fish == null || fish.unlockType != UnlockType.Quest) continue;
            if (p.IsUnlocked(fish.id)) continue;

            if (p.GetStat(fish.questType) >= fish.questTarget)
            {
                p.Unlock(fish.id);
                unlocked.Add(fish);
            }
        }
        return unlocked;
    }

    public static int GetProgress(FishData fish)
    {
        return SaveSystem.Profile.GetStat(fish.questType);
    }

    public static string QuestText(FishData fish)
    {
        switch (fish.questType)
        {
            case QuestType.BestScore:
                return $"ทำคะแนนในเกมเดียวให้ถึง {fish.questTarget:N0}";
            case QuestType.TotalMerges:
                return $"รวมร่างปลาให้ครบ {fish.questTarget:N0} ครั้ง";
            case QuestType.GamesPlayed:
                return $"เล่นให้ครบ {fish.questTarget:N0} เกม";
            case QuestType.TopMerges:
                return $"รวมปลาตัวสุดท้ายของ Deck ให้ครบ {fish.questTarget:N0} ครั้ง";
            default:
                return "";
        }
    }

    /// ข้อความ "ปลดล็อกยังไง" สำหรับแสดงในหน้าจัด Deck
    public static string UnlockInfo(FishData fish)
    {
        switch (fish.unlockType)
        {
            case UnlockType.Quest:
                return "เควสต์: " + QuestText(fish);
            case UnlockType.Gacha:
                return string.IsNullOrEmpty(fish.unlockHint)
                    ? "สุ่มได้จากกาชา"
                    : fish.unlockHint;
            default:
                return "";
        }
    }
}