using UnityEngine;

[CreateAssetMenu(menuName = "Fish/Abilities/Shockwave", fileName = "Ability_Shockwave")]
public class ShockwaveAbility : FishAbility
{
    public float radius = 2.5f;
    public float force = 6f;

    public override void OnMerged(Fish created, GameManager gm)
    {
        Vector2 center = created.transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);

        foreach (Collider2D hit in hits)
        {
            Rigidbody2D rb = hit.attachedRigidbody;
            if (rb == null || rb == created.Body) continue;

            Vector2 dir = ((Vector2)rb.position - center).normalized;
            rb.AddForce(dir * force, ForceMode2D.Impulse);
        }
    }
}