using UnityEngine;

[CreateAssetMenu(menuName = "Fish/Abilities/Bouncy", fileName = "Ability_Bouncy")]
public class BouncyAbility : FishAbility
{
    [Tooltip("สร้าง Physics Material 2D ที่ Bounciness ~0.6 แล้วลากมาใส่")]
    public PhysicsMaterial2D bouncyMaterial;

    public override void OnSpawned(Fish fish)
    {
        if (bouncyMaterial != null)
        {
            fish.Body.sharedMaterial = bouncyMaterial;
        }
    }
}