using System.Collections;
using UnityEngine;

/// Fish_13 Giant Mekong Catfish — On Merge (created)
/// ดูดปลารอบตัวเข้าหาตัวเองและลงล่าง ทำให้กองปลาแน่นขึ้น
[CreateAssetMenu(menuName = "Fish/Abilities/Riverbed Vortex", fileName = "Ability_RiverbedVortex")]
public class RiverbedVortexAbility : FishAbility
{
    public float radius = 3.5f;
    [Tooltip("ความเร่งที่ดูด (หน่วย/วินาที²) — คูณมวลให้ปลาทุกขนาดขยับพอๆ กัน")]
    public float pullAcceleration = 22f;
    [Tooltip("น้ำหนักของทิศลงล่างเทียบกับทิศเข้าหาศูนย์กลาง")]
    public float downwardBias = 0.6f;
    public float duration = 0.4f;

    public override void OnMergeCreated(Fish created, GameManager gm)
    {
        gm.RaiseAbilityTriggered(this, created.transform.position);
        gm.StartCoroutine(Pull(created, gm));
    }

    private IEnumerator Pull(Fish created, GameManager gm)
    {
        Vector2 center = created.transform.position;
        float t = 0f;

        while (t < duration && gm != null && !gm.IsGameOver)
        {
            if (created != null && created.IsInPlay)
            {
                center = created.Body.position;
            }

            foreach (Fish f in gm.FishInRadius(center, radius, created))
            {
                Vector2 toCenter = center - f.Body.position;
                Vector2 dir = toCenter.sqrMagnitude > 0.0001f ? toCenter.normalized : Vector2.zero;
                dir = (dir + Vector2.down * downwardBias).normalized;
                f.Body.AddForce(dir * pullAcceleration * f.Body.mass);
            }

            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
        }
    }
}
