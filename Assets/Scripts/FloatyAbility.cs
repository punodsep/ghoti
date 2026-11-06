using UnityEngine;

[CreateAssetMenu(menuName = "Fish/Abilities/Floaty", fileName = "Ability_Floaty")]
public class FloatyAbility : FishAbility
{
    public float gravityScale = 0.5f;

    public override void OnSpawned(Fish fish)
    {
        fish.Body.gravityScale = gravityScale;
    }
}