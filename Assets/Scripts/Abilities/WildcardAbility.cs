using UnityEngine;

/// Fish_10 Snakehead — On Collision
/// รวมกับปลาที่ต่ำกว่า 1 layer ได้ด้วย ผลลัพธ์คือ layer ถัดไปของตัวที่ใหญ่กว่า
/// (On Merge (score) ของทั้งสองตัวทำงาน — จัดการใน GameManager.MergeFish)
[CreateAssetMenu(menuName = "Fish/Abilities/Wildcard", fileName = "Ability_Wildcard")]
public class WildcardAbility : FishAbility
{
    public int layersBelow = 1;

    public override bool CanMergeWith(Fish self, Fish other)
    {
        int diff = self.Tier - other.Tier;
        return diff > 0 && diff <= layersBelow;
    }
}
