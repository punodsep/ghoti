using UnityEngine;

/// Fish_17 Koi — On Merge (created)
/// ปลาตัวถัดไปที่จะได้ (ที่โชว์ใน HUD) กลายเป็น layer 1 เสมอ
[CreateAssetMenu(menuName = "Fish/Abilities/Lucky Koi", fileName = "Ability_LuckyKoi")]
public class LuckyKoiAbility : FishAbility
{
    [Tooltip("0 = layer 1 (ตัวเล็กสุด)")]
    public int forcedTier = 0;

    public override void OnMergeCreated(Fish created, GameManager gm)
    {
        gm.SetUpcoming(0, forcedTier);
        gm.RaiseAbilityTriggered(this, created.transform.position);
    }
}
