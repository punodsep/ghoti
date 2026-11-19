using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Fish_19 Electric Eel — On Collision
/// Eel รวมทันทีที่แตะ Eel อีกตัว แล้วไฟฟ้ากระโดดไปรวมปลาอีก 1 คู่ที่ตรงกันใกล้ๆ
/// (การรวมที่ไฟช็อตสั่งนับเป็นการรวมปกติ ถ้าคู่นั้นเป็น Eel อีกก็ช็อตต่อได้ — ติดเพดานต่อการปล่อย)
[CreateAssetMenu(menuName = "Fish/Abilities/Chain Zap", fileName = "Ability_ChainZap")]
public class ChainZapAbility : FishAbility
{
    [Tooltip("รัศมีจากจุดรวมที่ไฟช็อตกระโดดไปหาคู่ได้")]
    public float jumpRadius = 3f;
    [Tooltip("คู่ที่ขอบห่างกันไม่เกินนี้ถูกช็อตให้รวมได้ (ไม่ต้องแตะกันจริง)")]
    public float maxPairGap = 0.35f;

    public override void OnMergeScore(Fish self, MergeContext ctx, GameManager gm)
    {
        gm.RaiseAbilityTriggered(this, ctx.position);
        gm.StartCoroutine(Zap(ctx.position, gm));
    }

    private IEnumerator Zap(Vector2 origin, GameManager gm)
    {
        // รอให้การรวมรอบนี้จบ (ปลาใหม่เกิด ปลาเก่าถูกลบ) ก่อนหาเป้าหมาย
        yield return new WaitForFixedUpdate();
        if (gm == null || gm.IsGameOver) yield break;

        List<Fish> near = gm.FishInRadius(origin, jumpRadius);
        Fish bestA = null, bestB = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < near.Count; i++)
        {
            for (int j = i + 1; j < near.Count; j++)
            {
                Fish a = near[i];
                Fish b = near[j];
                if (!gm.CanMerge(a, b)) continue;

                float gap = Vector2.Distance(a.Body.position, b.Body.position) - a.Radius - b.Radius;
                if (gap > maxPairGap) continue;

                Vector2 mid = (a.Body.position + b.Body.position) * 0.5f;
                float dist = Vector2.Distance(origin, mid);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestA = a;
                    bestB = b;
                }
            }
        }

        if (bestA == null) yield break;

        // ให้ตัวที่เข้าโหลก่อนเป็น a (ตามกติกา tie-break)
        if (bestA.SpawnOrder > bestB.SpawnOrder)
        {
            (bestA, bestB) = (bestB, bestA);
        }

        gm.RaiseAbilityTriggered(this, (bestA.Body.position + bestB.Body.position) * 0.5f);
        gm.TryAbilityMerge(bestA, bestB);
    }
}
