using System;
using System.Collections.Generic;

[Serializable]
public class PlayerProfile
{
    public int version = 1;
    public int coins = 300;          // เหรียญเริ่มต้น = กาชา 3 ครั้ง

    // สถิติ (ใช้กับเควสต์)
    public int bestScore;
    public int totalMerges;
    public int gamesPlayed;
    public int topMerges;

    // กาชา
    public int gachaPulls;
    public int pityCounter;

    public List<string> unlockedFishIds = new List<string>();
    public List<string> deckFishIds = new List<string>();

    public bool IsUnlocked(string id) => unlockedFishIds.Contains(id);

    /// คืน true ถ้าเป็นการปลดล็อกครั้งแรก
    public bool Unlock(string id)
    {
        if (string.IsNullOrEmpty(id) || IsUnlocked(id)) return false;
        unlockedFishIds.Add(id);
        return true;
    }

    public int GetStat(QuestType type)
    {
        switch (type)
        {
            case QuestType.BestScore: return bestScore;
            case QuestType.TotalMerges: return totalMerges;
            case QuestType.GamesPlayed: return gamesPlayed;
            case QuestType.TopMerges: return topMerges;
            default: return 0;
        }
    }

    public List<FishData> GetDeck()
    {
        List<FishData> deck = new List<FishData>();
        FishDatabase db = FishDatabase.Instance;
        if (db == null) return deck;

        foreach (string id in deckFishIds)
        {
            FishData f = db.Get(id);
            if (f != null) deck.Add(f);
        }
        return deck;
    }
}