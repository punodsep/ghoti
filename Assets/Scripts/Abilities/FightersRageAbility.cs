using UnityEngine;

/// Fish_15 Betta — On Merge (score)
/// 5 วินาทีหลัง Betta รวม ทุกการรวมได้คะแนน x1.5 (ถ้าเกิดซ้ำ = รีเฟรชเวลาเต็ม ไม่บวกเวลา)
[CreateAssetMenu(menuName = "Fish/Abilities/Fighter's Rage", fileName = "Ability_FightersRage")]
public class FightersRageAbility : FishAbility
{
    public float multiplier = 1.5f;
    public float duration = 5f;

    public override void OnMergeScore(Fish self, MergeContext ctx, GameManager gm)
    {
        gm.StartTimedMultiplier(this, multiplier, duration);
        gm.RaiseAbilityTriggered(this, ctx.position);
    }
}
