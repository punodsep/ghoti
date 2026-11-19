using UnityEngine;

/// Fish_11 Swordfish — On Merge (created)
/// เมื่อเกิดจากการรวม จะแทงปลาตัวที่เล็กที่สุดในระยะใกล้ให้หายไป (ไม่ได้คะแนน) เพื่อเปิดที่ว่าง
[CreateAssetMenu(menuName = "Fish/Abilities/Pierce", fileName = "Ability_Pierce")]
public class PierceAbility : FishAbility
{
    [Tooltip("ระยะจากขอบตัว Swordfish")]
    public float range = 1.2f;

    public override void OnMergeCreated(Fish created, GameManager gm)
    {
        Vector2 center = created.transform.position;
        Fish target = null;
        float bestDist = float.MaxValue;

        foreach (Fish f in gm.FishInRadius(center, created.Radius + range, created))
        {
            if (f.Tier >= created.Tier) continue;

            float dist = Vector2.Distance(center, f.Body.position);
            bool better = target == null ||
                          f.Tier < target.Tier ||
                          (f.Tier == target.Tier && dist < bestDist);
            if (better)
            {
                target = f;
                bestDist = dist;
            }
        }

        if (target == null) return;

        gm.RaiseAbilityTriggered(this, target.Body.position);
        gm.RemoveFish(target);
    }
}
