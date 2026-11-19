using UnityEngine;

/// Fish_12 Arowana — On Merge (score)
/// คะแนน x1.5 และ +1 เหรียญ ทุกครั้งที่รวม (เหรียญได้ตอนจบเกม)
[CreateAssetMenu(menuName = "Fish/Abilities/Golden Scales", fileName = "Ability_GoldenScales")]
public class GoldenScalesAbility : FishAbility
{
    public float multiplier = 1.5f;
    public int coins = 1;

    public override void OnMergeScore(Fish self, MergeContext ctx, GameManager gm)
    {
        ctx.multiplier *= multiplier;
        ctx.bonusCoins += coins;
        gm.RaiseAbilityTriggered(this, ctx.position);
    }
}
