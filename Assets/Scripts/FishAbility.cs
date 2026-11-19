using UnityEngine;

/// ข้อมูลของการรวมร่างหนึ่งครั้ง ส่งให้ ability ปรับคะแนน/เหรียญได้
public class MergeContext
{
    public Fish a;
    public Fish b;
    public Vector2 position;
    public int resultTier;          // tier ของปลาที่จะเกิด (== deck.Count แปลว่า Top Merge)
    public bool isTopMerge;
    public bool causedByAbility;
    public int baseScore;
    public float multiplier = 1f;   // ทุกตัวคูณซ้อนกัน (GDD: Everything stacks)
    public int bonusCoins;
}

/// Ability ของปลา 1 ตัว ลำดับการทำงานใน 1 การรวมร่าง (ตาม GDD):
/// OnCollision → รวมร่าง → OnMergeScore ของคู่ที่รวม → ปลาใหม่เกิด → OnSpawned → OnMergeCreated
public abstract class FishAbility : ScriptableObject
{
    [Header("ข้อมูลที่แสดงใน UI")]
    public string abilityName;
    [TextArea] public string description;

    /// ทุกครั้งที่ปลาตัวนี้เกิด (ทั้งตอนถือรอปล่อย และตอนเกิดจากการรวม)
    public virtual void OnSpawned(Fish fish) { }

    /// ปลาตัวนี้แต่ละตัวในโหล ทำให้ HUD แสดงปลาตัวถัดไปเพิ่มกี่ตัว (Angelfish)
    public virtual int ExtraUpcomingPreviews => 0;

    /// เมื่อปลาตัวนี้ (ปล่อยแล้ว) เริ่มชนปลาตัวอื่น
    public virtual void OnCollision(Fish self, Fish other, GameManager gm) { }

    /// อนุญาตให้รวมกับปลาต่าง tier ได้ไหม (เช่น Snakehead: รวมกับ tier ต่ำกว่า 1 ขั้น)
    public virtual bool CanMergeWith(Fish self, Fish other) => false;

    /// เมื่อปลาตัวนี้เป็นหนึ่งในคู่ที่รวมร่าง: ปรับคะแนน/เหรียญ/เริ่ม effect
    public virtual void OnMergeScore(Fish self, MergeContext ctx, GameManager gm) { }

    /// เมื่อการรวมร่างสร้างปลาตัวนี้ขึ้นมา
    public virtual void OnMergeCreated(Fish created, GameManager gm) { }
}
