using UnityEngine;

public enum UnlockType { Default, Quest, Gacha }
public enum FishRarity { Common, Rare, Epic, Legendary }
public enum QuestType { BestScore, TotalMerges, GamesPlayed, TopMerges }

[CreateAssetMenu(menuName = "Fish/Fish Data", fileName = "Fish_")]
public class FishData : ScriptableObject
{
    public string id;                 // เช่น "clownfish" (ใช้ตอน save)
    public string displayName;
    public Sprite sprite;
    public FishRarity rarity;
    public FishAbility ability;       // ว่างได้ = ไม่มีความสามารถ

    [Header("Unlock")]
    public UnlockType unlockType;
    [TextArea] public string unlockHint;
    public QuestType questType;
    public int questTarget;
}