using System.Collections.Generic;
using UnityEngine;

/// Fish_14 Sardine — On Merge (score)
/// +10% คะแนนต่อปลา layer เดียวกันอีกหนึ่งตัวที่แตะคู่ที่กำลังรวม
[CreateAssetMenu(menuName = "Fish/Abilities/School", fileName = "Ability_School")]
public class SchoolAbility : FishAbility
{
    public float bonusPerFish = 0.1f;
    [Tooltip("ระยะห่างของขอบตัวที่ยังนับว่า 'แตะ'")]
    public float touchTolerance = 0.08f;

    public override void OnMergeScore(Fish self, MergeContext ctx, GameManager gm)
    {
        HashSet<Fish> school = new HashSet<Fish>();
        AddTouching(ctx.a, ctx, gm, school);
        AddTouching(ctx.b, ctx, gm, school);

        if (school.Count == 0) return;

        ctx.multiplier *= 1f + bonusPerFish * school.Count;
        gm.RaiseAbilityTriggered(this, ctx.position);
    }

    private void AddTouching(Fish member, MergeContext ctx, GameManager gm, HashSet<Fish> school)
    {
        if (member.Ability != this) return;

        Vector2 pos = member.Body.position;
        foreach (Fish f in gm.FishInRadius(pos, member.Radius + touchTolerance, member))
        {
            if (f == ctx.a || f == ctx.b) continue;
            if (f.Tier != member.Tier) continue;
            school.Add(f);
        }
    }
}
