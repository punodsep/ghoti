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

    [Header("Merging layer")]
    [Tooltip("ช่องใน Deck ที่ปลาตัวนี้ใส่ได้ (1 = เล็กสุด, 7 = ใหญ่สุด)")]
    [Range(1, GameSession.MaxDeck)] public int layer = 1;

    [Header("Hitbox (สัดส่วนของด้านที่ยาวที่สุดของสไปรต์)")]
    [Tooltip("รัศมีวงกลมที่พอดีตัวปลาจริง — Tools/Fish/Fit Hitboxes To Sprites คำนวณให้")]
    [Range(0.05f, 0.5f)] public float hitboxRadius = 0.5f;
    [Tooltip("จุดศูนย์กลางตัวปลาเทียบกับกลางสไปรต์ (y ขึ้น)")]
    public Vector2 hitboxOffset;

    [Header("Unlock")]
    public UnlockType unlockType;
    [TextArea] public string unlockHint;
    public QuestType questType;
    public int questTarget;
}