using UnityEngine;

/// Fish_9 Octopus — On Collision
/// ปลาที่ Octopus แตะ ช้าลง 50% เป็นเวลา 1 วินาที ช่วยให้กองปลานิ่ง
[CreateAssetMenu(menuName = "Fish/Abilities/Sticky Tentacles", fileName = "Ability_StickyTentacles")]
public class StickyTentaclesAbility : FishAbility
{
    [Range(0f, 1f)] public float speedFactor = 0.5f;
    public float duration = 1f;

    public override void OnCollision(Fish self, Fish other, GameManager gm)
    {
        other.Slow(speedFactor, duration);
        gm.RaiseAbilityTriggered(this, other.Body.position);
    }
}
