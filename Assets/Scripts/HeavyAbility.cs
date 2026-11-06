using UnityEngine;

[CreateAssetMenu(menuName = "Fish/Abilities/Heavy", fileName = "Ability_Heavy")]
public class HeavyAbility : FishAbility
{
    public float gravityScale = 1.6f;
    public float massMultiplier = 2f;

    public override void OnSpawned(Fish fish)
    {
        fish.Body.gravityScale = gravityScale;
        fish.Body.mass *= massMultiplier;
    }
}