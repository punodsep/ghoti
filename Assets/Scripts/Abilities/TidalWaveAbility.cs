using UnityEngine;

/// Fish_20 Whale Shark — On Merge (created)
/// คลื่นใหญ่ ดันปลาทุกตัวในโหลลงล่างและออกด้านข้าง ห่างจากเส้นอันตราย แล้วโหลค่อยๆ นิ่ง
[CreateAssetMenu(menuName = "Fish/Abilities/Tidal Wave", fileName = "Ability_TidalWave")]
public class TidalWaveAbility : FishAbility
{
    [Tooltip("แรงกระแทก (คูณมวลให้ปลาทุกขนาดขยับพอๆ กัน)")]
    public float impulse = 7f;
    [Tooltip("สัดส่วนแรงด้านข้าง เทียบกับแรงลงล่าง")]
    public float sidewaysRatio = 0.35f;
    [Tooltip("ปลาที่สูงกว่า Whale Shark ได้แรงเพิ่มสูงสุดเท่านี้")]
    public float extraForHigherFish = 0.75f;
    public float heightForMaxExtra = 4f;

    public override void OnMergeCreated(Fish created, GameManager gm)
    {
        Vector2 center = created.transform.position;
        gm.RaiseAbilityTriggered(this, center);

        foreach (Fish f in gm.ActiveFish)
        {
            if (f == null || f == created || !f.IsInPlay) continue;

            Vector2 pos = f.Body.position;
            float side = Mathf.Sign(pos.x - center.x) * sidewaysRatio;
            Vector2 dir = new Vector2(side, -1f).normalized;

            float above = Mathf.Clamp01((pos.y - center.y) / heightForMaxExtra);
            float strength = impulse * (1f + extraForHigherFish * above);

            f.Body.AddForce(dir * strength * f.Body.mass, ForceMode2D.Impulse);
        }
    }
}
