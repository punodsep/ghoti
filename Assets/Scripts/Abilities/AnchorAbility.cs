using UnityEngine;

/// Fish_16 Seahorse — On Spawn
/// หนัก (gravity x1.6, มวล x2) และไม่กลิ้ง เป็นฐานที่มั่นคง
[CreateAssetMenu(menuName = "Fish/Abilities/Anchor", fileName = "Ability_Anchor")]
public class AnchorAbility : FishAbility
{
    public float gravityScale = 1.6f;
    public float massMultiplier = 2f;

    public override void OnSpawned(Fish fish)
    {
        fish.Body.gravityScale = gravityScale;
        fish.Body.mass *= massMultiplier;
        fish.Body.constraints |= RigidbodyConstraints2D.FreezeRotation;
    }
}
